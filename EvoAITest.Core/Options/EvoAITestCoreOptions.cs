namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for EvoAITest.Core services.
/// </summary>
/// <remarks>
/// <para>
/// This configuration class supports multiple LLM providers:
/// - Azure OpenAI (gpt-5.2-chat): Production deployment using Azure AI services with Key Vault for secrets
/// - Ollama: Local development with open-source models (qwen3:30b)
/// - Local: Custom HTTP LLM endpoints for specialized deployments
/// </para>
/// <para>
/// Configure these options in appsettings.json under the "EvoAITest:Core" section.
/// For production, sensitive values like API keys should be stored in Azure Key Vault
/// and accessed via Azure.Identity with managed identity authentication.
/// </para>
/// <para>
/// Example Development Configuration (Ollama):
/// <code>
/// {
///   "EvoAITest": {
///     "Core": {
///       "LLMProvider": "Ollama",
///       "OllamaEndpoint": "http://localhost:11434",
///       "OllamaModel": "qwen3:30b"
///     }
///   }
/// }
/// </code>
/// </para>
/// <para>
/// Example Production Configuration (Azure OpenAI):
/// <code>
/// {
///   "EvoAITest": {
///     "Core": {
///       "LLMProvider": "AzureOpenAI",
///       "LLMModel": "gpt-5.2-chat",
///       "AzureOpenAIDeployment": "gpt-5.2-chat",
///       "AzureOpenAIApiVersion": "2024-10-21"
///     }
///   }
/// }
/// // Note: AZURE_OPENAI_ENDPOINT comes from environment variable
/// // Note: AZURE_OPENAI_API_KEY comes from Azure Key Vault secret "LLMAPIKEY"
/// </code>
/// </para>
/// </remarks>
public sealed class EvoAITestCoreOptions
{
    // ============================================================
    // LLM Provider Configuration
    // ============================================================

    /// <summary>
    /// Gets or sets the LLM provider to use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Valid values:
    /// - "AzureOpenAI": Use Azure OpenAI Service with gpt-5.2-chat (production)
    /// - "Ollama": Use local Ollama server with qwen3:30b (development)
    /// - "Local": Use custom local HTTP LLM endpoint
    /// </para>
    /// <para>
    /// Default: "AzureOpenAI"
    /// </para>
    /// <para>
    /// For production deployments, use AzureOpenAI with managed identity for secure access.
    /// For local development, use Ollama with open-source models.
    /// </para>
    /// </remarks>
    public string LLMProvider { get; set; } = "AzureOpenAI";

    /// <summary>
    /// Gets or sets the LLM model to use (applies to Azure OpenAI only).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: "gpt-5.2-chat"
    /// </para>
    /// <para>
    /// This setting is only used when LLMProvider is "AzureOpenAI".
    /// For Ollama and Local providers, the model is determined by the endpoint configuration.
    /// </para>
    /// </remarks>
    public string LLMModel { get; set; } = "gpt-5.2-chat";

    // ============================================================
    // Azure OpenAI Configuration
    // ============================================================

    /// <summary>
    /// Gets or sets the Azure OpenAI endpoint URL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example: "https://youropenai.cognitiveservices.azure.com"
    /// </para>
    /// <para>
    /// This should be set via the AZURE_OPENAI_ENDPOINT environment variable for security.
    /// Do not include a trailing slash.
    /// </para>
    /// <para>
    /// Required when LLMProvider is "AzureOpenAI".
    /// </para>
    /// <para>
    /// Environment Variable: AZURE_OPENAI_ENDPOINT
    /// </para>
    /// </remarks>
    public string AzureOpenAIEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure OpenAI deployment name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: "gpt-5.2-chat"
    /// </para>
    /// <para>
    /// This is the name of your deployment in the Azure OpenAI resource.
    /// It may differ from the model name depending on your Azure configuration.
    /// </para>
    /// <para>
    /// Required when LLMProvider is "AzureOpenAI".
    /// </para>
    /// </remarks>
    public string AzureOpenAIDeployment { get; set; } = "gpt-5.2-chat";

    /// <summary>
    /// Gets or sets the Azure OpenAI API key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SECURITY: This should NEVER be hardcoded or stored in appsettings.json.
    /// </para>
    /// <para>
    /// Production Approach (Recommended):
    /// - Store as "LLMAPIKEY" secret in Azure Key Vault (e.g., evoai-keyvault)
    /// - Use Azure.Identity with DefaultAzureCredential() for authentication
    /// - Configure Key Vault in Program.cs using AddAzureKeyVault()
    /// </para>
    /// <para>
    /// Development Approach (Alternative):
    /// - Set AZURE_OPENAI_API_KEY environment variable
    /// - Use User Secrets in development (dotnet user-secrets set)
    /// </para>
    /// <para>
    /// Example Key Vault Configuration:
    /// <code>
    /// var keyVaultUrl = "https://evoai-keyvault.vault.azure.net/";
    /// builder.Configuration.AddAzureKeyVault(
    ///     new Uri(keyVaultUrl),
    ///     new DefaultAzureCredential()
    /// );
    /// </code>
    /// </para>
    /// <para>
    /// Required when LLMProvider is "AzureOpenAI".
    /// </para>
    /// </remarks>
    public string AzureOpenAIApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure OpenAI API version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: "2024-10-21"
    /// </para>
    /// <para>
    /// This specifies which version of the Azure OpenAI REST API to use.
    /// Keep this updated to access the latest GPT-5 features.
    /// </para>
    /// <para>
    /// See Azure OpenAI documentation for available versions:
    /// https://learn.microsoft.com/en-us/azure/ai-services/openai/reference
    /// </para>
    /// </remarks>
    public string AzureOpenAIApiVersion { get; set; } = "2024-10-21";

    // ============================================================
    // Ollama Configuration (Local Development)
    // ============================================================

    /// <summary>
    /// Gets or sets the Ollama server endpoint URL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: "http://localhost:11434"
    /// </para>
    /// <para>
    /// Used only when LLMProvider is "Ollama".
    /// Ollama must be installed and running locally for this to work.
    /// </para>
    /// <para>
    /// Installation: https://ollama.ai
    /// Start Ollama: `ollama serve`
    /// </para>
    /// <para>
    /// Can be overridden via environment variable OLLAMA_ENDPOINT for containerized development.
    /// </para>
    /// </remarks>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Gets or sets the Ollama model name (e.g., "qwen2.5-7b", "llama3", "mistral").
    /// </summary>
    public string OllamaModel { get; set; } = "qwen2.5-7b";

    // ============================================================
    // Local LLM Configuration (Custom Endpoints)
    // ============================================================

    /// <summary>
    /// Gets or sets the custom local LLM endpoint URL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used only when LLMProvider is "Local".
    /// </para>
    /// <para>
    /// This should be the full URL to your custom LLM server's chat completions endpoint.
    /// The endpoint must be compatible with OpenAI's /v1/chat/completions API format.
    /// </para>
    /// <para>
    /// Example: "http://localhost:8080/v1/chat/completions"
    /// </para>
    /// <para>
    /// This is useful for specialized deployments, custom models, or internal LLM services.
    /// </para>
    /// </remarks>
    public string LocalLLMEndpoint { get; set; } = string.Empty;

    // ============================================================
    // Browser Automation Configuration
    // ============================================================

    /// <summary>
    /// Gets or sets the default timeout for browser operations in milliseconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 30000 (30 seconds)
    /// </para>
    /// <para>
    /// This timeout applies to browser navigation, element waits, and action execution.
    /// Increase for slow websites or networks. Minimum value: 1000ms.
    /// </para>
    /// <para>
    /// For production with slow external sites, consider 60000ms or higher.
    /// </para>
    /// </remarks>
    public int BrowserTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets a value indicating whether to run browsers in headless mode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: true
    /// </para>
    /// <para>
    /// Headless mode runs browsers without a visible UI, which is faster and required
    /// for containerized deployments (Azure Container Apps, Kubernetes, etc.).
    /// </para>
    /// <para>
    /// Set to false for local development/debugging to see browser actions in real-time.
    /// </para>
    /// </remarks>
    public bool HeadlessMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 3
    /// </para>
    /// <para>
    /// When browser operations fail (element not found, timeout, etc.), they will be
    /// retried up to this many times with exponential backoff.
    /// </para>
    /// <para>
    /// Minimum value: 1 (no retries, just one attempt)
    /// </para>
    /// </remarks>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the output path for screenshots captured during automation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: "/tmp/screenshots"
    /// </para>
    /// <para>
    /// Screenshots are captured on errors and for debugging purposes.
    /// Ensure this directory is writable by the application.
    /// </para>
    /// <para>
    /// For Windows: Use "C:\\temp\\screenshots" or similar
    /// For Linux/Containers: Use "/tmp/screenshots" or persistent volume mount
    /// For Azure Container Apps: Mount Azure Files or use blob storage
    /// </para>
    /// </remarks>
    public string ScreenshotOutputPath { get; set; } = "/tmp/screenshots";

    // ============================================================
    // Observability Configuration
    // ============================================================

    /// <summary>
    /// Gets or sets the log level for EvoAITest.Core operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: "Information"
    /// </para>
    /// <para>
    /// Valid values: "Trace", "Debug", "Information", "Warning", "Error", "Critical"
    /// </para>
    /// <para>
    /// For production, use "Information" or "Warning".
    /// For debugging, use "Debug" or "Trace".
    /// </para>
    /// </remarks>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets a value indicating whether to enable OpenTelemetry telemetry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: true
    /// </para>
    /// <para>
    /// When enabled, metrics, traces, and logs are exported to configured OpenTelemetry endpoints.
    /// This integrates with Aspire Dashboard and Azure Monitor for observability.
    /// </para>
    /// <para>
    /// Set to false only for local development or if you have a different monitoring solution.
    /// </para>
    /// </remarks>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for OpenTelemetry identification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: "EvoAITest.Core"
    /// </para>
    /// <para>
    /// This name appears in traces, metrics, and logs to identify the source service.
    /// Useful when multiple services are deployed in the same environment.
    /// </para>
    /// </remarks>
    public string ServiceName { get; set; } = "EvoAITest.Core";

    // ============================================================
    // Computed Properties
    // ============================================================

    /// <summary>
    /// Gets a value indicating whether Azure OpenAI is the configured provider.
    /// </summary>
    public bool IsAzureOpenAI => LLMProvider == "AzureOpenAI";

    /// <summary>
    /// Gets a value indicating whether Ollama is the configured provider.
    /// </summary>
    public bool IsOllama => LLMProvider == "Ollama";

    /// <summary>
    /// Gets a value indicating whether a custom local LLM endpoint is configured.
    /// </summary>
    public bool IsLocalLLM => LLMProvider == "Local";

    // ============================================================
    // Validation
    // ============================================================

    /// <summary>
    /// Validates the configuration and throws an exception if any required settings are missing or invalid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method during application startup to fail fast if configuration is incorrect.
    /// </para>
    /// <para>
    /// Example usage in Program.cs:
    /// <code>
    /// var options = app.Services.GetRequiredService&lt;IOptions&lt;EvoAITestCoreOptions&gt;&gt;().Value;
    /// options.ValidateConfiguration();
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration values are missing or invalid for the selected provider.
    /// </exception>
    public void ValidateConfiguration()
    {
        // Validate LLM Provider-specific settings
        switch (LLMProvider)
        {
            case "AzureOpenAI":
                if (string.IsNullOrWhiteSpace(AzureOpenAIEndpoint))
                {
                    throw new InvalidOperationException(
                        "Azure OpenAI endpoint is required when LLMProvider is 'AzureOpenAI'. " +
                        "Set the AZURE_OPENAI_ENDPOINT environment variable. " +
                        "Example: AZURE_OPENAI_ENDPOINT=https://youropenai.cognitiveservices.azure.com");
                }

                if (string.IsNullOrWhiteSpace(AzureOpenAIApiKey))
                {
                    throw new InvalidOperationException(
                        "Azure OpenAI API key is required when LLMProvider is 'AzureOpenAI'. " +
                        "Configure Key Vault secret 'LLMAPIKEY' and use DefaultAzureCredential() for authentication. " +
                        "For local development, set AZURE_OPENAI_API_KEY environment variable or use User Secrets. " +
                        "NEVER hardcode API keys in configuration files.");
                }

                if (string.IsNullOrWhiteSpace(AzureOpenAIDeployment))
                {
                    throw new InvalidOperationException(
                        "Azure OpenAI deployment name is required when LLMProvider is 'AzureOpenAI'. " +
                        "Set AzureOpenAIDeployment in configuration (e.g., 'gpt-5').");
                }
                break;

            case "Ollama":
                if (string.IsNullOrWhiteSpace(OllamaEndpoint))
                {
                    throw new InvalidOperationException(
                        "Ollama endpoint is required when LLMProvider is 'Ollama'. " +
                        "Ensure Ollama is installed and running at http://localhost:11434. " +
                        "Install from: https://ollama.ai");
                }

                if (string.IsNullOrWhiteSpace(OllamaModel))
                {
                    throw new InvalidOperationException(
                        "Ollama model is required when LLMProvider is 'Ollama'. " +
                        "Install a model with: ollama pull qwen3:30b");
                }
                break;

            case "Local":
                if (string.IsNullOrWhiteSpace(LocalLLMEndpoint))
                {
                    throw new InvalidOperationException(
                        "Local LLM endpoint is required when LLMProvider is 'Local'. " +
                        "Set LocalLLMEndpoint to your custom LLM server URL. " +
                        "Example: http://localhost:8080/v1/chat/completions");
                }
                break;

            default:
                throw new InvalidOperationException(
                    $"Invalid LLMProvider: '{LLMProvider}'. " +
                    "Valid values are: 'AzureOpenAI', 'Ollama', 'Local'.");
        }

        // Validate browser settings
        if (BrowserTimeoutMs < 1000)
        {
            throw new InvalidOperationException(
                $"BrowserTimeoutMs must be at least 1000ms. Current value: {BrowserTimeoutMs}ms. " +
                "Recommended: 30000ms for production, 10000ms for development.");
        }

        if (MaxRetries < 1)
        {
            throw new InvalidOperationException(
                $"MaxRetries must be at least 1. Current value: {MaxRetries}. " +
                "Recommended: 3 for production, 1 for development.");
        }

        // Validate screenshot path (warn if not set, don't fail)
        if (string.IsNullOrWhiteSpace(ScreenshotOutputPath))
        {
            throw new InvalidOperationException(
                "ScreenshotOutputPath is required. " +
                "Set to a writable directory path (e.g., '/tmp/screenshots' for Linux, 'C:\\\\temp\\\\screenshots' for Windows).");
        }
    }

    // ============================================================
    // Managed Identity and Key Vault Configuration (For Production)
    // ============================================================

    /// <summary>
    /// Gets or sets the Azure Key Vault endpoint for secure key storage.
    /// </summary>
    /// <remarks>
    /// When specified, API keys will be retrieved from Azure Key Vault instead of configuration.
    /// Format: https://{vault-name}.vault.azure.net/
    /// </remarks>
    public string? KeyVaultEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the name of the secret in Key Vault containing the Azure OpenAI API key.
    /// </summary>
    /// <value>Default is "AzureOpenAI-ApiKey".</value>
    public string KeyVaultSecretName { get; set; } = "AzureOpenAI-ApiKey";

    /// <summary>
    /// Gets or sets a value indicating whether to use managed identity for Azure OpenAI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, DefaultAzureCredential will be used for authentication.
    /// This is the recommended approach for production deployments in Azure.
    /// </para>
    /// <para>
    /// For local development without Azure managed identity configured, set this to false
    /// and provide an API key via <see cref="AzureOpenAIApiKey"/> or Key Vault instead.
    /// If enabled in an environment without managed identity, the factory will automatically
    /// fall back to API key authentication if available.
    /// </para>
    /// </remarks>
    public bool UseAzureOpenAIManagedIdentity { get; set; } = true;

    // ============================================================
    // Advanced Configuration
    // ============================================================

    /// <summary>
    /// Gets or sets a value indicating whether to enable multi-model routing.
    /// </summary>
    /// <remarks>
    /// When enabled, requests will be routed to different models based on task type
    /// (e.g., GPT-4 for planning, Qwen for code generation).
    /// </remarks>
    public bool EnableMultiModelRouting { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic fallback to secondary providers.
    /// </summary>
    /// <remarks>
    /// When enabled, requests will automatically fall back to Ollama if Azure OpenAI
    /// is rate-limited, offline, or experiencing errors.
    /// </remarks>
    public bool EnableProviderFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets the routing strategy to use when multi-model routing is enabled.
    /// </summary>
    /// <value>
    /// Valid values: "TaskBased", "CostOptimized". Default is "TaskBased".
    /// </value>
    public string RoutingStrategy { get; set; } = "TaskBased";

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// </summary>
    /// <remarks>
    /// Number of consecutive failures before a provider's circuit breaker opens.
    /// Default is 5 failures.
    /// </remarks>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker open duration in seconds.
    /// </summary>
    /// <remarks>
    /// How long the circuit stays open before attempting recovery.
    /// Default is 30 seconds.
    /// </remarks>
    public int CircuitBreakerOpenDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the request timeout in seconds for LLM operations.
    /// </summary>
    /// <value>Default is 60 seconds.</value>
    public int LLMRequestTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Validates the LLM configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void ValidateLLMConfiguration()
    {
        if (string.IsNullOrWhiteSpace(LLMProvider))
        {
            throw new InvalidOperationException("LLMProvider must be specified in configuration");
        }

        if (LLMProvider == "AzureOpenAI")
        {
            if (string.IsNullOrWhiteSpace(AzureOpenAIEndpoint))
            {
                throw new InvalidOperationException(
                    "AzureOpenAIEndpoint must be specified when using Azure OpenAI provider");
            }

            if (string.IsNullOrWhiteSpace(AzureOpenAIDeployment))
            {
                throw new InvalidOperationException(
                    "AzureOpenAIDeployment must be specified when using Azure OpenAI provider");
            }

            // API key is optional if using managed identity or Key Vault
            if (!UseAzureOpenAIManagedIdentity &&
                string.IsNullOrWhiteSpace(KeyVaultEndpoint) &&
                string.IsNullOrWhiteSpace(AzureOpenAIApiKey))
            {
                throw new InvalidOperationException(
                    "AzureOpenAIApiKey, KeyVaultEndpoint, or managed identity must be configured");
            }
        }
        else if (LLMProvider == "Ollama")
        {
            if (string.IsNullOrWhiteSpace(OllamaEndpoint))
            {
                throw new InvalidOperationException(
                    "OllamaEndpoint must be specified when using Ollama provider");
            }

            if (string.IsNullOrWhiteSpace(OllamaModel))
            {
                throw new InvalidOperationException(
                    "OllamaModel must be specified when using Ollama provider");
            }
        }
    }
}
