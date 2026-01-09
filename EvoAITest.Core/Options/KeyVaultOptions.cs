using System.ComponentModel.DataAnnotations;

namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for Azure Key Vault integration.
/// </summary>
/// <remarks>
/// <para>
/// These options control how the application connects to and retrieves secrets
/// from Azure Key Vault. The configuration can be provided via:
/// - appsettings.json
/// - Environment variables
/// - Azure App Configuration
/// - User secrets (local development)
/// </para>
/// <para>
/// Example configuration:
/// <code>
/// {
///   "KeyVault": {
///     "VaultUri": "https://my-keyvault.vault.azure.net/",
///     "EnableCaching": true,
///     "CacheDurationMinutes": 60,
///     "MaxRetries": 3,
///     "TenantId": "00000000-0000-0000-0000-000000000000"
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class KeyVaultOptions
{
    /// <summary>
    /// Gets or sets the Azure Key Vault URI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The URI should be in the format: https://{vault-name}.vault.azure.net/
    /// </para>
    /// <para>
    /// This is the only required configuration value. If not provided,
    /// the application will fail to start (when Key Vault is enabled).
    /// </para>
    /// <para>
    /// Example: https://evoaitest-kv.vault.azure.net/
    /// </para>
    /// </remarks>
    [Required(ErrorMessage = "Key Vault URI is required")]
    [Url(ErrorMessage = "Key Vault URI must be a valid HTTPS URL")]
    public string VaultUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to enable in-memory caching of secrets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled (default), retrieved secrets are cached in memory for the
    /// duration specified by <see cref="CacheDuration"/>. This reduces calls
    /// to Key Vault and improves performance.
    /// </para>
    /// <para>
    /// Disable caching if you need real-time secret rotation without restarting
    /// the application. However, this will increase latency and Key Vault costs.
    /// </para>
    /// <para>
    /// Default: true (recommended for production)
    /// </para>
    /// </remarks>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration for secrets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Secrets are cached for this duration before being fetched again from Key Vault.
    /// Longer durations reduce costs but may delay secret rotation.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - Development: 5-15 minutes
    /// - Production: 30-60 minutes
    /// - High-security: Disable caching
    /// </para>
    /// <para>
    /// Default: 60 minutes
    /// </para>
    /// </remarks>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed Key Vault operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The provider uses exponential backoff for retries. Transient failures
    /// (network issues, throttling) are automatically retried.
    /// </para>
    /// <para>
    /// Recommended: 3-5 retries
    /// </para>
    /// <para>
    /// Default: 3
    /// </para>
    /// </remarks>
    [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the Azure Active Directory tenant ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional. If not specified, the default tenant from Azure credentials is used.
    /// </para>
    /// <para>
    /// Specify this when:
    /// - Using a service principal from a different tenant
    /// - Accessing Key Vault across tenant boundaries
    /// - Explicit tenant specification required by security policy
    /// </para>
    /// <para>
    /// Format: GUID (e.g., "00000000-0000-0000-0000-000000000000")
    /// </para>
    /// </remarks>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Key Vault integration is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When false, the application will not attempt to connect to Key Vault.
    /// Secrets must be provided via other configuration sources.
    /// </para>
    /// <para>
    /// Set to false for:
    /// - Local development without Azure access
    /// - Testing environments
    /// - Non-Azure deployments
    /// </para>
    /// <para>
    /// Default: true
    /// </para>
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for Key Vault operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Maximum time to wait for a single Key Vault API call.
    /// Increase if experiencing timeout errors due to network latency.
    /// </para>
    /// <para>
    /// Recommended: 10-30 seconds
    /// </para>
    /// <para>
    /// Default: 30 seconds
    /// </para>
    /// </remarks>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to log secret names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, secret names (but not values) are included in log messages.
    /// This is useful for debugging but may be considered sensitive information
    /// in high-security environments.
    /// </para>
    /// <para>
    /// Secret values are NEVER logged regardless of this setting.
    /// </para>
    /// <para>
    /// Default: true
    /// </para>
    /// </remarks>
    public bool LogSecretNames { get; set; } = true;

    /// <summary>
    /// Validates the Key Vault configuration.
    /// </summary>
    /// <returns>
    /// A tuple containing:
    /// - bool: true if configuration is valid; otherwise, false.
    /// - List&lt;string&gt;: validation error messages (empty if valid).
    /// </returns>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (!Enabled)
        {
            // Skip validation if Key Vault is disabled
            return (true, errors);
        }

        if (string.IsNullOrWhiteSpace(VaultUri))
        {
            errors.Add("VaultUri is required when Key Vault is enabled");
        }
        else if (!Uri.TryCreate(VaultUri, UriKind.Absolute, out var uri) || uri.Scheme != "https")
        {
            errors.Add("VaultUri must be a valid HTTPS URL");
        }

        if (CacheDuration < TimeSpan.Zero)
        {
            errors.Add("CacheDuration must be a positive value");
        }

        if (CacheDuration > TimeSpan.FromHours(24))
        {
            errors.Add("CacheDuration should not exceed 24 hours");
        }

        if (MaxRetries < 0 || MaxRetries > 10)
        {
            errors.Add("MaxRetries must be between 0 and 10");
        }

        if (OperationTimeout < TimeSpan.FromSeconds(1))
        {
            errors.Add("OperationTimeout must be at least 1 second");
        }

        if (OperationTimeout > TimeSpan.FromMinutes(5))
        {
            errors.Add("OperationTimeout should not exceed 5 minutes");
        }

        if (!string.IsNullOrWhiteSpace(TenantId) && !Guid.TryParse(TenantId, out _))
        {
            errors.Add("TenantId must be a valid GUID");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Gets default configuration for local development.
    /// </summary>
    /// <remarks>
    /// Use this when Key Vault is not available in development and
    /// secrets are provided via user secrets or environment variables.
    /// </remarks>
    /// <returns>A KeyVaultOptions instance configured for local development.</returns>
    public static KeyVaultOptions CreateDevelopmentDefaults()
    {
        return new KeyVaultOptions
        {
            Enabled = false,
            EnableCaching = false,
            CacheDuration = TimeSpan.FromMinutes(5),
            MaxRetries = 1,
            LogSecretNames = true
        };
    }

    /// <summary>
    /// Gets default configuration for production environments.
    /// </summary>
    /// <param name="vaultUri">The Key Vault URI.</param>
    /// <param name="tenantId">Optional tenant ID.</param>
    /// <returns>A KeyVaultOptions instance configured for production.</returns>
    public static KeyVaultOptions CreateProductionDefaults(string vaultUri, string? tenantId = null)
    {
        return new KeyVaultOptions
        {
            VaultUri = vaultUri,
            TenantId = tenantId,
            Enabled = true,
            EnableCaching = true,
            CacheDuration = TimeSpan.FromHours(1),
            MaxRetries = 3,
            OperationTimeout = TimeSpan.FromSeconds(30),
            LogSecretNames = false // More secure in production
        };
    }
}
