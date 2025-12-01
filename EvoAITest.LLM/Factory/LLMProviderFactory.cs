using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using EvoAITest.Core.Options;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Providers;
using EvoAITest.LLM.Resilience;
using EvoAITest.LLM.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvoAITest.LLM.Factory;

/// <summary>
/// Factory for creating LLM provider instances based on configuration.
/// Supports Azure OpenAI, Ollama, custom local LLM providers, multi-model routing, and automatic fallback.
/// </summary>
/// <remarks>
/// <para>
/// The factory supports advanced features:
/// </para>
/// <list type="bullet">
/// <item><description>Multi-model routing - Route requests to appropriate models (GPT-4 for planning, Qwen for code)</description></item>
/// <item><description>Automatic fallback - Fall back to Ollama when Azure OpenAI is rate-limited or offline</description></item>
/// <item><description>Circuit breakers - Prevent cascading failures with automatic provider health management</description></item>
/// <item><description>Azure Key Vault - Secure API key storage with managed identity support</description></item>
/// <item><description>Streaming support - Enable real-time response streaming for large outputs</description></item>
/// </list>
/// </remarks>
public sealed class LLMProviderFactory
{
    private readonly EvoAITestCoreOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<LLMProviderFactory> _logger;
    private readonly SecretClient? _keyVaultClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMProviderFactory"/> class.
    /// </summary>
    /// <param name="options">Configuration options for EvoAITest.Core.</param>
    /// <param name="loggerFactory">Logger factory for creating provider-specific loggers.</param>
    public LLMProviderFactory(
        IOptions<EvoAITestCoreOptions> options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<LLMProviderFactory>();

        // Validate configuration on initialization
        try
        {
            _options.ValidateLLMConfiguration();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "LLM provider configuration is invalid");
            throw;
        }

        // Initialize Key Vault client if configured
        if (!string.IsNullOrWhiteSpace(_options.KeyVaultEndpoint))
        {
            try
            {
                _keyVaultClient = new SecretClient(
                    new Uri(_options.KeyVaultEndpoint),
                    new DefaultAzureCredential());

                _logger.LogInformation("Initialized Azure Key Vault client for secure key management");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Key Vault client");
            }
        }
    }

    /// <summary>
    /// Creates an LLM provider instance based on the configured provider type.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="ILLMProvider"/> based on configuration.
    /// If multi-model routing is enabled, returns a <see cref="RoutingLLMProvider"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configured provider type is unknown or when provider creation fails.
    /// </exception>
    public async Task<ILLMProvider> CreateProviderAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating LLM provider: {Provider}", _options.LLMProvider);

        try
        {
            // If multi-model routing is enabled, create routing provider
            if (_options.EnableMultiModelRouting || _options.EnableProviderFallback)
            {
                return await CreateRoutingProviderAsync(cancellationToken);
            }

            // Otherwise, create a single provider
            return await CreateSingleProviderAsync(_options.LLMProvider, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create LLM provider: {Provider}", _options.LLMProvider);
            
            // Preserve original InvalidOperationException, wrap others for context
            if (ex is InvalidOperationException)
                throw;
            
            throw new InvalidOperationException(
                $"Failed to create LLM provider '{_options.LLMProvider}'. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Creates a routing provider with multiple underlying providers and fallback support.
    /// </summary>
    private async Task<ILLMProvider> CreateRoutingProviderAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating routing provider with multi-model routing: {Enabled}, fallback: {Fallback}",
            _options.EnableMultiModelRouting, _options.EnableProviderFallback);

        var providers = new List<ILLMProvider>();

        // Always try to create primary provider
        try
        {
            var primaryProvider = await CreateSingleProviderAsync(_options.LLMProvider, cancellationToken);
            providers.Add(primaryProvider);
            _logger.LogInformation("Added primary provider: {Provider}", _options.LLMProvider);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create primary provider {Provider}", _options.LLMProvider);
        }

        // Add fallback provider if enabled
        if (_options.EnableProviderFallback)
        {
            var fallbackProviderType = _options.LLMProvider == "AzureOpenAI" ? "Ollama" : "AzureOpenAI";
            
            try
            {
                var fallbackProvider = await CreateSingleProviderAsync(fallbackProviderType, cancellationToken);
                providers.Add(fallbackProvider);
                _logger.LogInformation("Added fallback provider: {Provider}", fallbackProviderType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create fallback provider {Provider}", fallbackProviderType);
            }
        }

        if (providers.Count == 0)
        {
            throw new InvalidOperationException(
                "Failed to create any LLM providers. Check configuration and provider availability.");
        }

        // Create routing strategy
        IRoutingStrategy strategy = _options.RoutingStrategy switch
        {
            "TaskBased" => new TaskBasedRoutingStrategy(),
            "CostOptimized" => new CostOptimizedRoutingStrategy(),
            _ => new TaskBasedRoutingStrategy()
        };

        _logger.LogInformation("Using routing strategy: {Strategy}", strategy.Name);

        // Create circuit breaker registry
        var circuitBreakerOptions = new CircuitBreakerOptions
        {
            FailureThreshold = _options.CircuitBreakerFailureThreshold,
            OpenDuration = TimeSpan.FromSeconds(_options.CircuitBreakerOpenDurationSeconds),
            RequestTimeout = TimeSpan.FromSeconds(_options.LLMRequestTimeoutSeconds)
        };

        var circuitBreakers = new CircuitBreakerRegistry(
            circuitBreakerOptions,
            _loggerFactory.CreateLogger<CircuitBreaker>());

        // Create routing provider options
        var routingOptions = new RoutingProviderOptions
        {
            EnableFallback = _options.EnableProviderFallback,
            RequestTimeout = TimeSpan.FromSeconds(_options.LLMRequestTimeoutSeconds),
            MaxRetries = 3
        };

        return new RoutingLLMProvider(
            providers,
            strategy,
            circuitBreakers,
            routingOptions,
            _loggerFactory.CreateLogger<RoutingLLMProvider>());
    }

    /// <summary>
    /// Creates a single provider instance based on provider type.
    /// </summary>
    private async Task<ILLMProvider> CreateSingleProviderAsync(
        string providerType,
        CancellationToken cancellationToken)
    {
        return providerType switch
        {
            "AzureOpenAI" => await CreateAzureOpenAIProviderAsync(cancellationToken),
            "Ollama" => CreateOllamaProvider(),
            "Local" => CreateLocalProvider(),
            _ => throw new InvalidOperationException(
                $"Unknown LLM provider: '{providerType}'. " +
                "Valid values are: 'AzureOpenAI', 'Ollama', 'Local'.")
        };
    }

    /// <summary>
    /// Creates an Azure OpenAI provider instance.
    /// Supports managed identity, API key, and Azure Key Vault authentication.
    /// </summary>
    private async Task<ILLMProvider> CreateAzureOpenAIProviderAsync(CancellationToken cancellationToken)
    {
        var logger = _loggerFactory.CreateLogger<AzureOpenAIProvider>();

        // Try managed identity first (most secure)
        if (_options.UseAzureOpenAIManagedIdentity)
        {
            _logger.LogInformation(
                "Creating Azure OpenAI provider with Managed Identity. Endpoint: {Endpoint}, Deployment: {Deployment}",
                _options.AzureOpenAIEndpoint,
                _options.AzureOpenAIDeployment);

            return new AzureOpenAIProvider(
                _options.AzureOpenAIEndpoint,
                _options.AzureOpenAIDeployment,
                logger);
        }

        // Try Key Vault next
        if (_keyVaultClient != null && !string.IsNullOrWhiteSpace(_options.KeyVaultSecretName))
        {
            try
            {
                _logger.LogInformation(
                    "Retrieving Azure OpenAI API key from Key Vault secret: {SecretName}",
                    _options.KeyVaultSecretName);

                var secret = await _keyVaultClient.GetSecretAsync(_options.KeyVaultSecretName, cancellationToken: cancellationToken);
                var apiKey = secret.Value.Value;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogError(
                        "Retrieved API key from Key Vault is null, empty, or whitespace. Skipping provider creation and falling back to next authentication method.");
                }
                else
                {
                    _logger.LogInformation(
                        "Successfully retrieved API key from Key Vault. Creating Azure OpenAI provider.");

                    return new AzureOpenAIProvider(
                        _options.AzureOpenAIEndpoint,
                        apiKey,
                        _options.AzureOpenAIDeployment,
                        logger);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret from Key Vault, falling back to configuration");
            }
        }

        // Fall back to API key from configuration
        if (!string.IsNullOrWhiteSpace(_options.AzureOpenAIApiKey))
        {
            _logger.LogInformation(
                "Creating Azure OpenAI provider with API key from configuration. Endpoint: {Endpoint}, Deployment: {Deployment}",
                _options.AzureOpenAIEndpoint,
                _options.AzureOpenAIDeployment);

            _logger.LogWarning(
                "Using API key from configuration. For production, use Managed Identity or Key Vault for enhanced security.");

            return new AzureOpenAIProvider(
                _options.AzureOpenAIEndpoint,
                _options.AzureOpenAIApiKey,
                _options.AzureOpenAIDeployment,
                logger);
        }

        throw new InvalidOperationException(
            "No valid authentication method configured for Azure OpenAI. " +
            "Configure managed identity, Key Vault, or API key.");
    }

    /// <summary>
    /// Creates an Ollama provider instance for local development.
    /// </summary>
    private ILLMProvider CreateOllamaProvider()
    {
        var logger = _loggerFactory.CreateLogger<OllamaProvider>();

        _logger.LogInformation(
            "Creating Ollama provider. Endpoint: {Endpoint}, Model: {Model}",
            _options.OllamaEndpoint,
            _options.OllamaModel);

        return new OllamaProvider(
            _options.OllamaEndpoint,
            _options.OllamaModel,
            logger);
    }

    /// <summary>
    /// Creates a local LLM provider instance for custom endpoints.
    /// </summary>
    private ILLMProvider CreateLocalProvider()
    {
        var logger = _loggerFactory.CreateLogger<OllamaProvider>();

        _logger.LogInformation(
            "Creating local LLM provider. Endpoint: {Endpoint}",
            _options.LocalLLMEndpoint);

        _logger.LogWarning(
            "Local provider is using Ollama-compatible API. Ensure your custom endpoint supports the Ollama API format.");

        // For now, use OllamaProvider for custom local endpoints
        return new OllamaProvider(
            _options.LocalLLMEndpoint,
            _options.LLMModel ?? "custom-model",
            logger);
    }

    /// <summary>
    /// Legacy synchronous method for backward compatibility.
    /// </summary>
    /// <returns>An instance of <see cref="ILLMProvider"/> based on configuration.</returns>
    /// <remarks>
    /// This method uses Task.Run to avoid deadlocks in contexts with synchronization contexts.
    /// For new code, use CreateProviderAsync instead.
    /// </remarks>
    [Obsolete("Use CreateProviderAsync instead for proper async support")]
    public ILLMProvider CreateProvider()
    {
        // Avoid deadlocks by running async code on a thread pool thread.
        return Task.Run(() => CreateProviderAsync()).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the configured provider name.
    /// </summary>
    /// <returns>The name of the configured LLM provider.</returns>
    public string GetConfiguredProviderName() => _options.LLMProvider;

    /// <summary>
    /// Checks if the configured provider is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider is available; otherwise, false.</returns>
    public async Task<bool> IsProviderAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await CreateProviderAsync(cancellationToken);
            return await provider.IsAvailableAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check provider availability");
            return false;
        }
    }

    /// <summary>
    /// Gets information about the configured provider.
    /// </summary>
    /// <returns>A dictionary containing provider information.</returns>
    public Dictionary<string, string> GetProviderInfo()
    {
        var info = new Dictionary<string, string>
        {
            ["Provider"] = _options.LLMProvider,
            ["MultiModelRouting"] = _options.EnableMultiModelRouting.ToString(),
            ["FallbackEnabled"] = _options.EnableProviderFallback.ToString(),
            ["RoutingStrategy"] = _options.RoutingStrategy
        };

        if (_options.LLMProvider == "AzureOpenAI")
        {
            info["Endpoint"] = _options.AzureOpenAIEndpoint;
            info["Deployment"] = _options.AzureOpenAIDeployment;
            info["Model"] = _options.LLMModel;
            info["ApiVersion"] = _options.AzureOpenAIApiVersion;
            info["Authentication"] = _options.UseAzureOpenAIManagedIdentity
                ? "Managed Identity"
                : _keyVaultClient != null
                    ? "Key Vault"
                    : "API Key";
        }
        else if (_options.LLMProvider == "Ollama")
        {
            info["Endpoint"] = _options.OllamaEndpoint;
            info["Model"] = _options.OllamaModel;
            info["Cost"] = "Free";
        }
        else if (_options.LLMProvider == "Local")
        {
            info["Endpoint"] = _options.LocalLLMEndpoint;
            info["Model"] = _options.LLMModel ?? "custom-model";
        }

        return info;
    }
}
