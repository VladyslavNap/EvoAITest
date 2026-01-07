using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace EvoAITest.Core.Services;

/// <summary>
/// Azure Key Vault implementation of <see cref="ISecretProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// This provider retrieves secrets from Azure Key Vault using managed identity
/// or other Azure credentials. It includes:
/// - In-memory caching with configurable TTL
/// - Automatic retry with exponential backoff
/// - Thread-safe concurrent access
/// - Comprehensive telemetry
/// </para>
/// <para>
/// The provider uses <see cref="DefaultAzureCredential"/> which supports:
/// - Managed Identity (production)
/// - Azure CLI (local development)
/// - Visual Studio (local development)
/// - Environment variables
/// </para>
/// </remarks>
public sealed class KeyVaultSecretProvider : ISecretProvider
{
    private readonly SecretClient _client;
    private readonly KeyVaultOptions _options;
    private readonly ILogger<KeyVaultSecretProvider> _logger;
    
    /// <summary>
    /// In-memory cache for retrieved secrets.
    /// Key: secret name, Value: (secret value, expiration time)
    /// </summary>
    private readonly ConcurrentDictionary<string, (string Value, DateTimeOffset Expiration)> _cache;
    
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultSecretProvider"/> class.
    /// </summary>
    /// <param name="options">Key Vault configuration options.</param>
    /// <param name="logger">Logger for telemetry and diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Key Vault endpoint is not configured.
    /// </exception>
    public KeyVaultSecretProvider(
        IOptions<KeyVaultOptions> options,
        ILogger<KeyVaultSecretProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _options = options.Value;
        _logger = logger;
        _cache = new ConcurrentDictionary<string, (string, DateTimeOffset)>();

        // Validate configuration
        if (string.IsNullOrWhiteSpace(_options.VaultUri))
        {
            throw new InvalidOperationException(
                "Key Vault URI is not configured. Set KeyVault:VaultUri in configuration.");
        }

        try
        {
            var credential = CreateCredential();
            _client = new SecretClient(new Uri(_options.VaultUri), credential);

            _logger.LogInformation(
                "Initialized Azure Key Vault client for vault: {VaultUri}",
                _options.VaultUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Key Vault client");
            throw new InvalidOperationException("Failed to initialize Key Vault client", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName, nameof(secretName));

        // Check cache first
        if (TryGetFromCache(secretName, out var cachedValue))
        {
            _logger.LogDebug("Retrieved secret '{SecretName}' from cache", secretName);
            return cachedValue;
        }

        try
        {
            _logger.LogDebug("Fetching secret '{SecretName}' from Key Vault", secretName);

            var response = await _client.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            var secretValue = response.Value.Value;

            // Cache the retrieved secret
            CacheSecret(secretName, secretValue);

            _logger.LogInformation(
                "Successfully retrieved secret '{SecretName}' from Key Vault",
                secretName);

            return secretValue;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{SecretName}' not found in Key Vault", secretName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve secret '{SecretName}' from Key Vault",
                secretName);
            throw new InvalidOperationException(
                $"Failed to retrieve secret '{secretName}' from Key Vault", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> secretNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(secretNames, nameof(secretNames));

        var names = secretNames.ToList();
        var results = new Dictionary<string, string>();

        _logger.LogDebug(
            "Fetching {Count} secrets from Key Vault",
            names.Count);

        // Fetch secrets in parallel for better performance
        var tasks = names.Select(async name =>
        {
            try
            {
                var value = await GetSecretAsync(name, cancellationToken);
                return (name, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to retrieve secret '{SecretName}', skipping",
                    name);
                return (name, (string?)null);
            }
        });

        var secretResults = await Task.WhenAll(tasks);

        foreach (var (name, value) in secretResults)
        {
            if (!string.IsNullOrEmpty(value))
            {
                results[name] = value;
            }
        }

        _logger.LogInformation(
            "Retrieved {SuccessCount}/{TotalCount} secrets from Key Vault",
            results.Count,
            names.Count);

        return results;
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Key Vault availability");

            // Try to list properties to verify access
            // We only need to check if we can connect, not retrieve all secrets
            var propertiesPage = _client.GetPropertiesOfSecretsAsync(cancellationToken: cancellationToken);
            await using var enumerator = propertiesPage.GetAsyncEnumerator(cancellationToken);
            
            // Just try to get the first item (if any)
            await enumerator.MoveNextAsync();

            _logger.LogInformation("Key Vault is available and accessible");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Key Vault is not available or not accessible");
            return false;
        }
    }

    /// <inheritdoc/>
    public void InvalidateCache(string? secretName = null)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            // Clear all cache
            var count = _cache.Count;
            _cache.Clear();
            _logger.LogInformation("Invalidated all cached secrets ({Count} entries)", count);
        }
        else
        {
            // Clear specific entry
            if (_cache.TryRemove(secretName, out _))
            {
                _logger.LogDebug("Invalidated cached secret '{SecretName}'", secretName);
            }
        }
    }

    /// <summary>
    /// Tries to retrieve a secret from the in-memory cache.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="value">The cached secret value if found and not expired.</param>
    /// <returns>True if a valid cached value was found; otherwise, false.</returns>
    private bool TryGetFromCache(string secretName, out string? value)
    {
        if (!_options.EnableCaching)
        {
            value = null;
            return false;
        }

        if (_cache.TryGetValue(secretName, out var cached))
        {
            if (cached.Expiration > DateTimeOffset.UtcNow)
            {
                value = cached.Value;
                return true;
            }

            // Expired, remove from cache
            _cache.TryRemove(secretName, out _);
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Caches a secret value with TTL-based expiration.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="secretValue">The secret value to cache.</param>
    private void CacheSecret(string secretName, string secretValue)
    {
        if (!_options.EnableCaching)
        {
            return;
        }

        var expiration = DateTimeOffset.UtcNow.Add(_options.CacheDuration);
        _cache[secretName] = (secretValue, expiration);

        _logger.LogDebug(
            "Cached secret '{SecretName}' until {Expiration}",
            secretName,
            expiration);
    }

    /// <summary>
    /// Creates the appropriate Azure credential based on configuration.
    /// </summary>
    /// <returns>An Azure credential for authentication.</returns>
    private DefaultAzureCredential CreateCredential()
    {
        var options = new DefaultAzureCredentialOptions
        {
            // Exclude shared token cache to avoid issues in containers
            ExcludeSharedTokenCacheCredential = true,
            
            // Retry configuration
            Retry =
            {
                MaxRetries = _options.MaxRetries,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(10),
                Mode = Azure.Core.RetryMode.Exponential
            }
        };

        // If tenant ID is specified, configure it
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
        {
            options.TenantId = _options.TenantId;
            _logger.LogDebug("Using tenant ID: {TenantId}", _options.TenantId);
        }

        return new DefaultAzureCredential(options);
    }
}
