using EvoAITest.Core.Models;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using EvoAITest.LLM.Resilience;
using EvoAITest.LLM.Routing;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace EvoAITest.LLM.Providers;

/// <summary>
/// LLM provider that implements multi-model routing and automatic fallback with circuit breakers.
/// </summary>
/// <remarks>
/// <para>
/// This provider wraps multiple underlying LLM providers (Azure OpenAI, Ollama, etc.) and
/// intelligently routes requests based on task type, complexity, and provider availability.
/// </para>
/// <para>
/// Key features:
/// - Multi-model routing based on task type (e.g., GPT-4 for planning, Qwen for code)
/// - Automatic fallback when primary provider fails or is rate-limited
/// - Circuit breaker pattern to prevent cascading failures
/// - Streaming support for large responses
/// - Cost optimization through intelligent provider selection
/// </para>
/// </remarks>
public sealed class RoutingLLMProvider : ILLMProvider
{
    private readonly List<ILLMProvider> _providers;
    private readonly IRoutingStrategy _strategy;
    private readonly CircuitBreakerRegistry _circuitBreakers;
    private readonly ILogger<RoutingLLMProvider> _logger;
    private readonly RoutingProviderOptions _options;
    private TokenUsage _lastUsage = new(0, 0, 0);
    private ILLMProvider? _lastUsedProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingLLMProvider"/> class.
    /// </summary>
    /// <param name="providers">The list of available LLM providers to route between.</param>
    /// <param name="strategy">The routing strategy to use for provider selection.</param>
    /// <param name="circuitBreakers">Circuit breaker registry for provider health management.</param>
    /// <param name="options">Configuration options for routing behavior.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentException">Thrown when providers list is empty.</exception>
    public RoutingLLMProvider(
        List<ILLMProvider> providers,
        IRoutingStrategy strategy,
        CircuitBreakerRegistry circuitBreakers,
        RoutingProviderOptions options,
        ILogger<RoutingLLMProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(circuitBreakers);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (providers.Count == 0)
        {
            throw new ArgumentException("At least one provider must be configured", nameof(providers));
        }

        _providers = providers;
        _strategy = strategy;
        _circuitBreakers = circuitBreakers;
        _options = options;
        _logger = logger;

        _logger.LogInformation(
            "Initialized routing provider with {Count} providers using {Strategy} strategy",
            providers.Count, strategy.Name);
    }

    /// <inheritdoc/>
    public string Name => "Routing Provider";

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedModels => _providers
        .SelectMany(p => p.SupportedModels)
        .Distinct()
        .ToList();

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(
        string prompt,
        Dictionary<string, string>? variables = null,
        List<BrowserTool>? tools = null,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        // Create default routing context
        var context = new RoutingContext
        {
            TaskType = DetermineTaskType(prompt, tools),
            Complexity = EstimateComplexity(prompt, tools),
            RequiresFunctionCalling = tools?.Count > 0,
            AllowFallback = _options.EnableFallback
        };

        return await GenerateWithRoutingAsync(
            context,
            async provider => await provider.GenerateAsync(
                prompt, variables, tools, maxTokens, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ToolCall>> ParseToolCallsAsync(
        string response,
        CancellationToken cancellationToken = default)
    {
        // Tool call parsing doesn't need routing, use first available provider
        var provider = _providers.FirstOrDefault();
        if (provider == null)
        {
            _logger.LogWarning("No providers available for ParseToolCallsAsync - returning empty list");
            return new List<ToolCall>();
        }

        return await provider.ParseToolCallsAsync(response, cancellationToken);
    }

    /// <inheritdoc/>
    public string GetModelName()
    {
        // Return the name of the most recently used provider
        return _lastUsedProvider?.GetModelName() 
            ?? _providers.FirstOrDefault()?.GetModelName() 
            ?? "routing-provider";
    }

    /// <inheritdoc/>
    public TokenUsage GetLastTokenUsage() => _lastUsage;

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Provider is available if at least one underlying provider is available
        foreach (var provider in _providers)
        {
            var breaker = _circuitBreakers.GetOrCreateBreaker(provider.Name);
            if (breaker.IsRequestAllowed())
            {
                try
                {
                    if (await provider.IsAvailableAsync(cancellationToken))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Provider {Provider} availability check failed",
                        provider.Name);
                }
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<LLMResponse> CompleteAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = CreateRoutingContext(request);

        return await GenerateWithRoutingAsync(
            context,
            async provider => await provider.CompleteAsync(request, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = CreateRoutingContext(request);
        context.RequiresStreaming = true;

        var provider = await SelectProviderWithFallbackAsync(context, cancellationToken);
        if (provider == null)
        {
            _logger.LogError("No available provider supports streaming for this request");
            yield break;
        }

        _lastUsedProvider = provider;
        var breaker = _circuitBreakers.GetOrCreateBreaker(provider.Name);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.RequestTimeout);

        _logger.LogInformation(
            "Streaming completion with provider {Provider} (routing context: {TaskType}, {Complexity})",
            provider.Name, context.TaskType, context.Complexity);

        var hasError = false;
        IAsyncEnumerator<LLMStreamChunk>? enumerator = null;
        
        try
        {
            enumerator = provider.StreamCompleteAsync(request, cts.Token).GetAsyncEnumerator(cts.Token);
            
            while (true)
            {
                LLMStreamChunk chunk;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                    chunk = enumerator.Current;
                }
                catch (Exception ex)
                {
                    hasError = true;
                    _logger.LogError(ex, "Error during streaming completion with provider {Provider}", provider.Name);
                    breaker.RecordFailure(ex);
                    yield break;
                }

                yield return chunk;
            }
        }
        finally
        {
            if (enumerator != null)
            {
                await enumerator.DisposeAsync();
            }
        }

        if (!hasError)
        {
            breaker.RecordSuccess();
        }
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var context = new RoutingContext
        {
            TaskType = TaskType.Extraction,
            Complexity = ComplexityLevel.Low,
            AllowFallback = true
        };

        return await GenerateWithRoutingAsync(
            context,
            async provider => await provider.GenerateEmbeddingAsync(text, model, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc/>
    public ProviderCapabilities GetCapabilities()
    {
        // Return combined capabilities of all providers
        var allCapabilities = _providers.Select(p => p.GetCapabilities()).ToList();

        return new ProviderCapabilities
        {
            SupportsStreaming = allCapabilities.Any(c => c.SupportsStreaming),
            SupportsFunctionCalling = allCapabilities.Any(c => c.SupportsFunctionCalling),
            SupportsVision = allCapabilities.Any(c => c.SupportsVision),
            SupportsEmbeddings = allCapabilities.Any(c => c.SupportsEmbeddings),
            MaxContextTokens = allCapabilities.Max(c => c.MaxContextTokens),
            MaxOutputTokens = allCapabilities.Max(c => c.MaxOutputTokens)
        };
    }

    /// <summary>
    /// Generates a response using automatic provider routing and fallback.
    /// </summary>
    private async Task<T> GenerateWithRoutingAsync<T>(
        RoutingContext context,
        Func<ILLMProvider, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var attemptedProviders = new HashSet<string>();
        Exception? lastException = null;
        var maxAttempts = Math.Max(_providers.Count, _options.MaxRetries);

        while (attemptedProviders.Count < maxAttempts)
        {
            var provider = await SelectProviderWithFallbackAsync(context, cancellationToken);
            
            if (provider == null || attemptedProviders.Contains(provider.Name))
            {
                break; // No more providers to try
            }

            attemptedProviders.Add(provider.Name);
            var breaker = _circuitBreakers.GetOrCreateBreaker(provider.Name);

            if (!breaker.IsRequestAllowed())
            {
                _logger.LogWarning(
                    "Circuit breaker is open for {Provider}, trying fallback",
                    provider.Name);
                continue;
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.RequestTimeout);

                _logger.LogInformation(
                    "Attempting request with provider {Provider} (routing context: {TaskType}, {Complexity})",
                    provider.Name, context.TaskType, context.Complexity);

                var result = await operation(provider);
                
                breaker.RecordSuccess();
                _lastUsage = provider.GetLastTokenUsage();
                _lastUsedProvider = provider;

                _logger.LogInformation(
                    "Request completed successfully with {Provider}. Tokens: {Input}/{Output}, Cost: ${Cost:F4}",
                    provider.Name, _lastUsage.InputTokens, _lastUsage.OutputTokens, _lastUsage.EstimatedCostUSD);

                return result;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken != cancellationToken)
            {
                // Request timeout, not user cancellation
                _logger.LogWarning(ex,
                    "Request to {Provider} timed out after {Timeout}s",
                    provider.Name, _options.RequestTimeout.TotalSeconds);

                breaker.RecordFailure(ex);
                lastException = ex;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Request to {Provider} failed: {Message}",
                    provider.Name, ex.Message);

                breaker.RecordFailure(ex);
                lastException = ex;

                if (!context.AllowFallback)
                {
                    throw;
                }
            }
        }

        // All providers failed
        var message = $"All {attemptedProviders.Count} provider(s) failed. " +
                     $"Attempted: {string.Join(", ", attemptedProviders)}";
        
        _logger.LogError(lastException, message);

        throw new InvalidOperationException(message, lastException);
    }

    /// <summary>
    /// Selects a provider using the routing strategy and filters by circuit breaker state.
    /// </summary>
    private async Task<ILLMProvider?> SelectProviderWithFallbackAsync(
        RoutingContext context,
        CancellationToken cancellationToken)
    {
        // Filter providers by circuit breaker state
        var availableProviders = _providers
            .Where(p =>
            {
                var breaker = _circuitBreakers.GetOrCreateBreaker(p.Name);
                return breaker.IsRequestAllowed();
            })
            .ToList();

        if (availableProviders.Count == 0)
        {
            _logger.LogWarning("No providers available - all circuit breakers are open");
            return null;
        }

        // Use routing strategy to select best provider
        var selectedProvider = await _strategy.SelectProviderAsync(
            context,
            availableProviders,
            cancellationToken);

        if (selectedProvider != null)
        {
            _logger.LogDebug(
                "Selected provider {Provider} for task type {TaskType} with complexity {Complexity}",
                selectedProvider.Name, context.TaskType, context.Complexity);
        }

        return selectedProvider;
    }

    /// <summary>
    /// Creates a routing context from an LLM request.
    /// </summary>
    private RoutingContext CreateRoutingContext(LLMRequest request)
    {
        var context = new RoutingContext
        {
            AllowFallback = _options.EnableFallback
        };

        // Infer task type from message content if possible
        if (request.Messages.Count > 0)
        {
            var lastMessage = request.Messages.Last();
            var content = lastMessage.Content?.ToLowerInvariant() ?? string.Empty;
            
            if (content.Contains("plan") || content.Contains("steps"))
                context.TaskType = TaskType.Planning;
            else if (content.Contains("code") || content.Contains("implement"))
                context.TaskType = TaskType.CodeGeneration;
            else if (content.Contains("heal") || content.Contains("fix"))
                context.TaskType = TaskType.Healing;
        }

        if (request.Functions?.Count > 0)
        {
            context.RequiresFunctionCalling = true;
        }

        return context;
    }

    /// <summary>
    /// Determines the task type from the prompt content.
    /// </summary>
    private TaskType DetermineTaskType(string prompt, List<BrowserTool>? tools)
    {
        var lowerPrompt = prompt.ToLowerInvariant();

        if (lowerPrompt.Contains("plan") || lowerPrompt.Contains("steps") || lowerPrompt.Contains("workflow"))
            return TaskType.Planning;

        if (lowerPrompt.Contains("code") || lowerPrompt.Contains("function") || lowerPrompt.Contains("implement"))
            return TaskType.CodeGeneration;

        if (lowerPrompt.Contains("heal") || lowerPrompt.Contains("fix") || lowerPrompt.Contains("recover"))
            return TaskType.Healing;

        if (lowerPrompt.Contains("extract") || lowerPrompt.Contains("scrape") || lowerPrompt.Contains("data"))
            return TaskType.Extraction;

        if (tools?.Count > 0)
            return TaskType.Planning;

        return TaskType.General;
    }

    /// <summary>
    /// Estimates the complexity level from the prompt content.
    /// </summary>
    private ComplexityLevel EstimateComplexity(string prompt, List<BrowserTool>? tools)
    {
        var wordCount = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var toolCount = tools?.Count ?? 0;

        if (wordCount > 500 || toolCount > 10)
            return ComplexityLevel.Expert;

        if (wordCount > 200 || toolCount > 5)
            return ComplexityLevel.High;

        if (wordCount > 50 || toolCount > 2)
            return ComplexityLevel.Medium;

        return ComplexityLevel.Low;
    }
}

/// <summary>
/// Configuration options for routing provider behavior.
/// </summary>
public sealed class RoutingProviderOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic fallback to secondary providers.
    /// </summary>
    /// <value>Default is true.</value>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for individual requests.
    /// </summary>
    /// <value>Default is 60 seconds.</value>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    /// <value>Default is 3 attempts.</value>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to log detailed routing decisions.
    /// </summary>
    /// <value>Default is false for production performance.</value>
    public bool EnableDetailedLogging { get; set; } = false;
}
