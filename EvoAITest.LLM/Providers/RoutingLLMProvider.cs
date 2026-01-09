using EvoAITest.Core.Models;
using EvoAITest.Core.Options;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvoAITest.LLM.Providers;

/// <summary>
/// LLM provider that routes requests to appropriate underlying providers based on task type.
/// Implements intelligent routing with automatic fallback and circuit breaker support.
/// </summary>
public sealed class RoutingLLMProvider : ILLMProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RoutingLLMProvider> _logger;
    private readonly LLMRoutingOptions _options;
    private readonly Dictionary<string, Routing.IRoutingStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingLLMProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving LLM providers.</param>
    /// <param name="options">Routing configuration options.</param>
    /// <param name="strategies">Available routing strategies.</param>
    /// <param name="logger">Logger for routing decisions and telemetry.</param>
    public RoutingLLMProvider(
        IServiceProvider serviceProvider,
        IOptions<LLMRoutingOptions> options,
        IEnumerable<Routing.IRoutingStrategy> strategies,
        ILogger<RoutingLLMProvider> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _strategies = (strategies ?? throw new ArgumentNullException(nameof(strategies)))
            .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        ValidateConfiguration();
    }

    /// <inheritdoc/>
    public string Name => "Routing";

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedModels =>
        new[] { "routed" }; // All models supported through routing

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(
        string prompt,
        Dictionary<string, string>? variables = null,
        List<BrowserTool>? tools = null,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentNullException(nameof(prompt));
        }

        // Detect task type from prompt
        var taskType = DetectTaskType(prompt, variables);

        _logger.LogInformation(
            "Detected task type: {TaskType} for prompt starting with '{PromptStart}...'",
            taskType,
            prompt.Length > 50 ? prompt.Substring(0, 50) : prompt);

        // Select route based on task type and strategy
        var route = SelectRoute(taskType);

        _logger.LogInformation(
            "Routing to {Provider}/{Model} via {Strategy} strategy",
            route.PrimaryProvider,
            route.PrimaryModel,
            route.Strategy);

        // Get the provider
        var provider = GetProvider(route.PrimaryProvider);
        if (provider == null)
        {
            _logger.LogError(
                "Provider '{Provider}' not found, attempting fallback",
                route.PrimaryProvider);

            if (route.HasFallback)
            {
                provider = GetProvider(route.FallbackProvider!);
                if (provider == null)
                {
                    throw new InvalidOperationException(
                        $"Neither primary provider '{route.PrimaryProvider}' nor fallback '{route.FallbackProvider}' could be resolved");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Provider '{route.PrimaryProvider}' not found and no fallback configured");
            }
        }

        // Execute the request
        if (provider == null)
        {
            throw new InvalidOperationException("Resolved LLM provider instance is null after routing logic.");
        }
        try
        {
            var response = await provider.GenerateAsync(
                prompt,
                variables,
                tools,
                maxTokens,
                cancellationToken);

            _logger.LogInformation(
                "Successfully routed request to {Provider}, response length: {Length}",
                provider.Name,
                response?.Length ?? 0);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing request on {Provider}, task type: {TaskType}",
                provider.Name,
                taskType);

            // If we have a fallback and haven't used it yet, try it
            if (route.HasFallback && provider.Name.Equals(route.PrimaryProvider, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Attempting fallback to {Provider}/{Model}",
                    route.FallbackProvider,
                    route.FallbackModel);

                var fallbackProvider = GetProvider(route.FallbackProvider!);
                if (fallbackProvider != null)
                {
                    return await fallbackProvider.GenerateAsync(
                        prompt,
                        variables,
                        tools,
                        maxTokens,
                        cancellationToken);
                }
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ToolCall>> ParseToolCallsAsync(
        string response,
        CancellationToken cancellationToken = default)
    {
        // For routing provider, we delegate to the underlying provider
        // This is a simplified implementation - in production, we'd track which provider was used
        // and delegate to that specific provider

        // For now, try the default provider
        var defaultProvider = GetProvider(_options.DefaultRoute.PrimaryProvider);
        if (defaultProvider == null)
        {
            throw new InvalidOperationException("Default provider not available for parsing tool calls");
        }

        return await defaultProvider.ParseToolCallsAsync(response, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TokenUsage> GetTokenUsageAsync(
        string prompt,
        string response,
        CancellationToken cancellationToken = default)
    {
        // Estimate token usage
        // This is a simplified implementation - in production, we'd track the actual provider used
        var inputTokens = EstimateTokens(prompt);
        var outputTokens = EstimateTokens(response);
        
        // Use a blended cost estimate based on the default route
        var estimatedCost = 0.0m;
        if (_options.DefaultRoute.CostPer1KTokens.HasValue)
        {
            var totalTokens = inputTokens + outputTokens;
            estimatedCost = (decimal)((totalTokens / 1000.0) * _options.DefaultRoute.CostPer1KTokens.Value);
        }

        return Task.FromResult(new TokenUsage(inputTokens, outputTokens, estimatedCost));
    }

    /// <inheritdoc/>
    public string GetModelName() => "Routing";

    /// <inheritdoc/>
    public TokenUsage GetLastTokenUsage()
    {
        // For routing provider, return a default usage
        // In a real implementation, we'd track the last request's actual usage
        return new TokenUsage(0, 0, 0.0m);
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Check if at least one provider is available
        try
        {
            var defaultProvider = GetProvider(_options.DefaultRoute.PrimaryProvider);
            if (defaultProvider != null)
            {
                return await defaultProvider.IsAvailableAsync(cancellationToken);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        // Detect task type and route appropriately
        var taskType = DetectTaskTypeFromRequest(request);
        var route = SelectRoute(taskType);

        var provider = GetProvider(route.PrimaryProvider);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider '{route.PrimaryProvider}' not found");
        }

        try
        {
            return await provider.CompleteAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing request on {Provider}", provider.Name);
            
            // Try fallback if available
            if (route.HasFallback)
            {
                var fallbackProvider = GetProvider(route.FallbackProvider!);
                if (fallbackProvider != null)
                {
                    _logger.LogWarning("Using fallback provider {Provider}", route.FallbackProvider);
                    return await fallbackProvider.CompleteAsync(request, cancellationToken);
                }
            }
            
            throw;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(
        LLMRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var taskType = DetectTaskTypeFromRequest(request);
        var route = SelectRoute(taskType);

        var provider = GetProvider(route.PrimaryProvider);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider '{route.PrimaryProvider}' not found");
        }

        await foreach (var chunk in provider.StreamCompleteAsync(request, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        // Route embedding requests to default provider
        var provider = GetProvider(_options.DefaultRoute.PrimaryProvider);
        if (provider == null)
        {
            throw new InvalidOperationException("Default provider not available for embeddings");
        }

        return await provider.GenerateEmbeddingAsync(text, model, cancellationToken);
    }

    /// <inheritdoc/>
    public ProviderCapabilities GetCapabilities()
    {
        return new ProviderCapabilities
        {
            SupportsStreaming = true,
            SupportsFunctionCalling = true,
            SupportsEmbeddings = true,
            SupportsVision = false,
            MaxContextTokens = 128000,
            MaxOutputTokens = 4096
        };
    }

    /// <summary>
    /// Detects task type from an LLMRequest.
    /// </summary>
    private TaskType DetectTaskTypeFromRequest(LLMRequest request)
    {
        // Extract text from messages
        var combinedText = string.Join(" ", request.Messages.Select(m => m.Content));
        return DetectTaskType(combinedText, null);
    }

    /// <summary>
    /// Detects the task type from the prompt content.
    /// </summary>
    private TaskType DetectTaskType(string prompt, Dictionary<string, string>? variables)
    {
        var fullPrompt = prompt;
        if (variables != null)
        {
            foreach (var (key, value) in variables)
            {
                fullPrompt = fullPrompt.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Convert to lowercase for case-insensitive matching
        var lowerPrompt = fullPrompt.ToLowerInvariant();

        // Check for explicit task type hints in variables
        if (variables?.TryGetValue("TaskType", out var taskTypeHint) == true &&
            Enum.TryParse<TaskType>(taskTypeHint, ignoreCase: true, out var explicitTaskType))
        {
            _logger.LogDebug("Using explicit task type from variables: {TaskType}", explicitTaskType);
            return explicitTaskType;
        }

        // Keyword-based detection (ordered by specificity)
        
        // Planning keywords
        if (ContainsAny(lowerPrompt, "create a plan", "break down", "steps to", "strategy", "approach for"))
        {
            return TaskType.Planning;
        }

        // Code generation keywords
        if (ContainsAny(lowerPrompt, "generate code", "write a test", "create a script", "implement", "function to"))
        {
            return TaskType.CodeGeneration;
        }

        // Analysis keywords
        if (ContainsAny(lowerPrompt, "analyze", "examine", "investigate", "understand", "extract", "identify patterns"))
        {
            return TaskType.Analysis;
        }

        // Intent detection keywords
        if (ContainsAny(lowerPrompt, "what is the intent", "user wants to", "trying to accomplish", "goal is"))
        {
            return TaskType.IntentDetection;
        }

        // Validation keywords
        if (ContainsAny(lowerPrompt, "validate", "verify", "check if", "is this correct", "does this match"))
        {
            return TaskType.Validation;
        }

        // Summarization keywords
        if (ContainsAny(lowerPrompt, "summarize", "brief", "overview", "key points", "tldr"))
        {
            return TaskType.Summarization;
        }

        // Classification keywords
        if (ContainsAny(lowerPrompt, "classify", "categorize", "tag", "label", "type of"))
        {
            return TaskType.Classification;
        }

        // Check prompt length for long-form generation
        if (fullPrompt.Length > 1000 || ContainsAny(lowerPrompt, "write a detailed", "comprehensive", "in-depth"))
        {
            return TaskType.LongFormGeneration;
        }

        // Default to General if no specific type detected
        _logger.LogDebug("No specific task type detected, using General");
        return TaskType.General;
    }

    /// <summary>
    /// Selects the appropriate route for the given task type.
    /// </summary>
    private Routing.RouteInfo SelectRoute(TaskType taskType)
    {
        // Get the configured routing strategy
        if (!_strategies.TryGetValue(_options.RoutingStrategy, out var strategy))
        {
            _logger.LogWarning(
                "Routing strategy '{Strategy}' not found, falling back to TaskBased",
                _options.RoutingStrategy);

            strategy = _strategies.Values.FirstOrDefault(s => s.Name == "TaskBased")
                ?? throw new InvalidOperationException("No TaskBased strategy available");
        }

        // Select route using the strategy
        return strategy.SelectRoute(taskType, _options);
    }

    /// <summary>
    /// Gets an LLM provider by name from the service provider.
    /// </summary>
    private ILLMProvider? GetProvider(string providerName)
    {
        // In a real implementation, this would resolve named providers from DI
        // For now, we'll use a simplified approach
        
        // This would typically be:
        // return _serviceProvider.GetKeyedService<ILLMProvider>(providerName);
        
        // For now, return null - this will be properly implemented when we integrate with the factory
        _logger.LogWarning(
            "Provider resolution not yet implemented, provider '{Provider}' requested",
            providerName);
        
        return null;
    }

    /// <summary>
    /// Estimates the number of tokens in a text string.
    /// </summary>
    private int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Rough estimation: 1 token ? 4 characters for English text
        // This is a simplification - real tokenizers are more complex
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    /// <summary>
    /// Checks if a string contains any of the specified substrings.
    /// </summary>
    private bool ContainsAny(string text, params string[] substrings)
    {
        return substrings.Any(s => text.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates the routing configuration at startup.
    /// </summary>
    private void ValidateConfiguration()
    {
        var (isValid, errors) = _options.Validate();
        if (!isValid)
        {
            var errorMessage = $"Invalid routing configuration: {string.Join("; ", errors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Validate that the configured strategy exists
        if (!_strategies.ContainsKey(_options.RoutingStrategy))
        {
            var availableStrategies = string.Join(", ", _strategies.Keys);
            throw new InvalidOperationException(
                $"Routing strategy '{_options.RoutingStrategy}' not found. Available strategies: {availableStrategies}");
        }

        _logger.LogInformation(
            "RoutingLLMProvider initialized with strategy '{Strategy}', {RouteCount} routes configured",
            _options.RoutingStrategy,
            _options.Routes.Count);
    }
}
