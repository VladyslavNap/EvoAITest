namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Provides secure access to application secrets from various sources.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction allows the application to retrieve sensitive configuration values
/// (API keys, connection strings, certificates) from secure storage providers like
/// Azure Key Vault, AWS Secrets Manager, or local development secrets.
/// </para>
/// <para>
/// Implementations should handle:
/// - Secure credential management
/// - In-memory caching for performance
/// - Automatic retry on transient failures
/// - Telemetry and logging (without exposing secret values)
/// </para>
/// </remarks>
public interface ISecretProvider
{
    /// <summary>
    /// Retrieves a single secret value by name.
    /// </summary>
    /// <param name="secretName">
    /// The name/key of the secret to retrieve.
    /// For Azure Key Vault, this is the secret identifier (e.g., "OpenAI-ApiKey").
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the secret value as a string, or null if not found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned value may be cached in memory for performance.
    /// Implementations should not log or expose the actual secret value.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var apiKey = await secretProvider.GetSecretAsync("OpenAI-ApiKey");
    /// if (string.IsNullOrEmpty(apiKey))
    /// {
    ///     throw new InvalidOperationException("API key not found");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="secretName"/> is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the secret provider is not properly configured or unavailable.
    /// </exception>
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves multiple secrets in a single batch operation.
    /// </summary>
    /// <param name="secretNames">
    /// The collection of secret names/keys to retrieve.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a dictionary mapping secret names to their values.
    /// Missing secrets will not be included in the dictionary.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Batch retrieval can be more efficient than individual calls when fetching
    /// multiple secrets. The implementation may parallelize requests or use
    /// provider-specific batch APIs.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var secrets = await secretProvider.GetSecretsAsync(new[] 
    /// { 
    ///     "OpenAI-ApiKey", 
    ///     "Database-ConnectionString" 
    /// });
    /// 
    /// if (secrets.TryGetValue("OpenAI-ApiKey", out var apiKey))
    /// {
    ///     // Use the API key
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="secretNames"/> is null.
    /// </exception>
    Task<Dictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> secretNames, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the secret provider is available and properly configured.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is true if the provider can access secrets; otherwise, false.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a health check on the secret provider without
    /// attempting to retrieve any actual secrets. It verifies:
    /// - The provider is configured with valid credentials
    /// - The provider endpoint is reachable
    /// - The application has necessary permissions
    /// </para>
    /// <para>
    /// Use this method at application startup to ensure the secret provider
    /// is operational before attempting to retrieve secrets.
    /// </para>
    /// </remarks>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cache for a specific secret, forcing a fresh retrieval on next access.
    /// </summary>
    /// <param name="secretName">
    /// The name of the secret whose cache entry should be invalidated.
    /// If null or empty, invalidates all cached secrets.
    /// </param>
    /// <remarks>
    /// <para>
    /// Use this method when you know a secret has been updated in the backing store
    /// and want to ensure the application retrieves the latest value.
    /// </para>
    /// <para>
    /// This is particularly useful in scenarios where secrets are rotated:
    /// <code>
    /// // After rotating a secret in Key Vault
    /// secretProvider.InvalidateCache("OpenAI-ApiKey");
    /// 
    /// // Next call will fetch the updated value
    /// var newKey = await secretProvider.GetSecretAsync("OpenAI-ApiKey");
    /// </code>
    /// </para>
    /// </remarks>
    void InvalidateCache(string? secretName = null);
}
