namespace EvoAITest.LLM.Factory;

using EvoAITest.Core.Options;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Factory for creating LLM provider instances based on configuration.
/// Supports Azure OpenAI, Ollama, and custom local LLM providers.
/// </summary>
public sealed class LLMProviderFactory
{
    private readonly EvoAITestCoreOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<LLMProviderFactory> _logger;

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
            _options.ValidateConfiguration();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "LLM provider configuration is invalid");
            throw;
        }
    }

    /// <summary>
    /// Creates an LLM provider instance based on the configured provider type.
    /// </summary>
    /// <returns>An instance of <see cref="ILLMProvider"/> based on configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configured provider type is unknown or when provider creation fails.
    /// </exception>
    public ILLMProvider CreateProvider()
    {
        _logger.LogInformation("Creating LLM provider: {Provider}", _options.LLMProvider);

        try
        {
            return _options.LLMProvider switch
            {
                "AzureOpenAI" => CreateAzureOpenAIProvider(),
                "Ollama" => CreateOllamaProvider(),
                "Local" => CreateLocalProvider(),
                _ => throw new InvalidOperationException(
                    $"Unknown LLM provider: '{_options.LLMProvider}'. " +
                    "Valid values are: 'AzureOpenAI', 'Ollama', 'Local'.")
            };
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
    /// Creates an Azure OpenAI provider instance.
    /// Prefers managed identity authentication when API key is not provided.
    /// </summary>
    /// <returns>An instance of <see cref="AzureOpenAIProvider"/>.</returns>
    private ILLMProvider CreateAzureOpenAIProvider()
    {
        var logger = _loggerFactory.CreateLogger<AzureOpenAIProvider>();

        // Prefer managed identity for production (no API key)
        if (string.IsNullOrWhiteSpace(_options.AzureOpenAIApiKey))
        {
            _logger.LogInformation(
                "Creating Azure OpenAI provider with Managed Identity authentication. Endpoint: {Endpoint}, Deployment: {Deployment}",
                _options.AzureOpenAIEndpoint,
                _options.AzureOpenAIDeployment);

            return new AzureOpenAIProvider(
                _options.AzureOpenAIEndpoint,
                _options.AzureOpenAIDeployment,
                logger);
        }

        // Fall back to API key authentication
        _logger.LogInformation(
            "Creating Azure OpenAI provider with API key authentication. Endpoint: {Endpoint}, Deployment: {Deployment}",
            _options.AzureOpenAIEndpoint,
            _options.AzureOpenAIDeployment);

        _logger.LogWarning(
            "Using API key authentication. For production, consider using Managed Identity for enhanced security.");

        return new AzureOpenAIProvider(
            _options.AzureOpenAIEndpoint,
            _options.AzureOpenAIApiKey,
            _options.AzureOpenAIDeployment,
            logger);
    }

    /// <summary>
    /// Creates an Ollama provider instance for local development.
    /// </summary>
    /// <returns>An instance of <see cref="OllamaProvider"/>.</returns>
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
    /// <returns>An instance of <see cref="OllamaProvider"/> configured for custom endpoint.</returns>
    /// <remarks>
    /// Currently uses OllamaProvider for local endpoints that follow the Ollama API format.
    /// In the future, this could be extended to support other custom LLM providers.
    /// </remarks>
    private ILLMProvider CreateLocalProvider()
    {
        var logger = _loggerFactory.CreateLogger<OllamaProvider>();

        _logger.LogInformation(
            "Creating local LLM provider. Endpoint: {Endpoint}",
            _options.LocalLLMEndpoint);

        _logger.LogWarning(
            "Local provider is using Ollama-compatible API. Ensure your custom endpoint supports the Ollama API format.");

        // For now, use OllamaProvider for custom local endpoints
        // In the future, this could be a separate LocalLLMProvider class
        return new OllamaProvider(
            _options.LocalLLMEndpoint,
            _options.LLMModel ?? "custom-model",
            logger);
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
            var provider = CreateProvider();
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
        return _options.LLMProvider switch
        {
            "AzureOpenAI" => new Dictionary<string, string>
            {
                ["Provider"] = "Azure OpenAI",
                ["Endpoint"] = _options.AzureOpenAIEndpoint,
                ["Deployment"] = _options.AzureOpenAIDeployment,
                ["Model"] = _options.LLMModel,
                ["ApiVersion"] = _options.AzureOpenAIApiVersion,
                ["Authentication"] = string.IsNullOrWhiteSpace(_options.AzureOpenAIApiKey)
                    ? "Managed Identity"
                    : "API Key"
            },
            "Ollama" => new Dictionary<string, string>
            {
                ["Provider"] = "Ollama",
                ["Endpoint"] = _options.OllamaEndpoint,
                ["Model"] = _options.OllamaModel,
                ["Cost"] = "Free"
            },
            "Local" => new Dictionary<string, string>
            {
                ["Provider"] = "Local LLM",
                ["Endpoint"] = _options.LocalLLMEndpoint,
                ["Model"] = _options.LLMModel ?? "custom-model"
            },
            _ => new Dictionary<string, string>
            {
                ["Provider"] = _options.LLMProvider,
                ["Status"] = "Unknown"
            }
        };
    }
}
