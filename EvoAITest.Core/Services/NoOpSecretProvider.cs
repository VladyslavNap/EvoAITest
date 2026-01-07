using EvoAITest.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services;

/// <summary>
/// A no-operation secret provider for development environments without Azure Key Vault.
/// </summary>
/// <remarks>
/// <para>
/// This provider is used when Key Vault is disabled (development, testing) and secrets
/// are provided via other configuration sources (user secrets, environment variables).
/// </para>
/// <para>
/// All operations return null or empty results. Secrets must be configured through:
/// - appsettings.json (not recommended for production)
/// - User Secrets (recommended for local development)
/// - Environment Variables (recommended for containers)
/// - Azure App Configuration (alternative to Key Vault)
/// </para>
/// </remarks>
public sealed class NoOpSecretProvider : ISecretProvider
{
    private readonly ILogger<NoOpSecretProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoOpSecretProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public NoOpSecretProvider(ILogger<NoOpSecretProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogWarning(
            "NoOpSecretProvider is active. Azure Key Vault is disabled. " +
            "Secrets must be provided via appsettings, user secrets, or environment variables.");
    }

    /// <inheritdoc/>
    public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "GetSecretAsync called for '{SecretName}' but NoOpSecretProvider returns null. " +
            "Configure secret via appsettings, user secrets, or enable Key Vault.",
            secretName);
        
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc/>
    public Task<Dictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> secretNames,
        CancellationToken cancellationToken = default)
    {
        var names = secretNames.ToList();
        
        _logger.LogDebug(
            "GetSecretsAsync called for {Count} secrets but NoOpSecretProvider returns empty dictionary. " +
            "Configure secrets via appsettings, user secrets, or enable Key Vault.",
            names.Count);
        
        return Task.FromResult(new Dictionary<string, string>());
    }

    /// <inheritdoc/>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("IsAvailableAsync: NoOpSecretProvider is always available but returns no secrets");
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public void InvalidateCache(string? secretName = null)
    {
        // No-op: nothing to invalidate
        _logger.LogDebug("InvalidateCache called but NoOpSecretProvider has no cache");
    }
}
