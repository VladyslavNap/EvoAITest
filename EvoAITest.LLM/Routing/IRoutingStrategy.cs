using EvoAITest.LLM.Abstractions;

namespace EvoAITest.LLM.Routing;

/// <summary>
/// Defines a strategy for routing LLM requests to appropriate providers based on context.
/// </summary>
/// <remarks>
/// <para>
/// Routing strategies determine which LLM provider should handle a request based on
/// factors such as task type, complexity, availability, cost, and latency requirements.
/// </para>
/// <para>
/// Implementations should be stateless and thread-safe as they may be called concurrently
/// from multiple threads.
/// </para>
/// </remarks>
public interface IRoutingStrategy
{
    /// <summary>
    /// Gets the name of this routing strategy.
    /// </summary>
    /// <value>
    /// A descriptive name identifying this routing strategy (e.g., "Task-Based Routing",
    /// "Cost-Optimized Routing", "Latency-Aware Routing").
    /// </value>
    string Name { get; }

    /// <summary>
    /// Selects the most appropriate provider for the given routing context.
    /// </summary>
    /// <param name="context">
    /// The routing context containing information about the request such as task type,
    /// complexity, streaming requirements, and priority.
    /// </param>
    /// <param name="availableProviders">
    /// The list of available LLM providers to choose from. Providers should already
    /// be filtered for availability if needed.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the routing operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the selected <see cref="ILLMProvider"/>, or null if no suitable provider
    /// could be found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The strategy should consider multiple factors when selecting a provider:
    /// - Task type and complexity
    /// - Provider capabilities (streaming, function calling, etc.)
    /// - Latency requirements
    /// - Cost considerations
    /// - Provider health and availability
    /// </para>
    /// <para>
    /// If no suitable provider is found, the method should return null rather than
    /// throwing an exception. The router will handle fallback logic.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="context"/> or <paramref name="availableProviders"/> is null.
    /// </exception>
    Task<ILLMProvider?> SelectProviderAsync(
        RoutingContext context,
        IReadOnlyList<ILLMProvider> availableProviders,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the priority score for a provider given the routing context.
    /// </summary>
    /// <param name="context">
    /// The routing context containing information about the request.
    /// </param>
    /// <param name="provider">
    /// The provider to score. The provider should be available and capable of
    /// handling the request.
    /// </param>
    /// <returns>
    /// A score from 0.0 (lowest priority) to 1.0 (highest priority) indicating
    /// how well this provider matches the routing context. Return 0.0 if the
    /// provider is not suitable for this context.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is used to rank providers when multiple providers are available.
    /// Higher scores indicate better matches. A score of 0.0 means the provider
    /// should not be used for this request.
    /// </para>
    /// <para>
    /// Scoring should be fast and deterministic. Avoid making network calls or
    /// performing expensive operations in this method.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="context"/> or <paramref name="provider"/> is null.
    /// </exception>
    double ScoreProvider(RoutingContext context, ILLMProvider provider);
}

/// <summary>
/// Default implementation of <see cref="IRoutingStrategy"/> that routes based on task type.
/// </summary>
/// <remarks>
/// <para>
/// This strategy routes requests to providers based on predefined rules:
/// - Planning tasks → GPT-4 (Azure OpenAI)
/// - Code generation → Qwen2.5 (Ollama)
/// - Simple tasks → Local models for cost savings
/// - Complex reasoning → Most capable model available
/// </para>
/// <para>
/// The strategy also considers provider capabilities (streaming, function calling)
/// and automatically filters out incompatible providers.
/// </para>
/// </remarks>
public sealed class TaskBasedRoutingStrategy : IRoutingStrategy
{
    /// <inheritdoc/>
    public string Name => "Task-Based Routing";

    /// <inheritdoc/>
    public Task<ILLMProvider?> SelectProviderAsync(
        RoutingContext context,
        IReadOnlyList<ILLMProvider> availableProviders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(availableProviders);

        if (availableProviders.Count == 0)
        {
            return Task.FromResult<ILLMProvider?>(null);
        }

        // Score all providers and select the highest scoring one
        var scores = availableProviders
            .Select(p => new { Provider = p, Score = ScoreProvider(context, p) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        return Task.FromResult(scores.FirstOrDefault()?.Provider);
    }

    /// <inheritdoc/>
    public double ScoreProvider(RoutingContext context, ILLMProvider provider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(provider);

        var score = 0.0;

        // Get provider capabilities
        var capabilities = provider.GetCapabilities();

        // Check required capabilities
        if (context.RequiresStreaming && !capabilities.SupportsStreaming)
        {
            return 0.0; // Provider cannot handle this request
        }

        if (context.RequiresFunctionCalling && !capabilities.SupportsFunctionCalling)
        {
            return 0.0; // Provider cannot handle this request
        }

        // Score based on task type
        score += ScoreForTaskType(context.TaskType, provider);

        // Score based on complexity
        score += ScoreForComplexity(context.Complexity, capabilities);

        // Score based on priority
        score += ScoreForPriority(context.Priority, provider);

        // Normalize to 0-1 range
        return Math.Clamp(score / 3.0, 0.0, 1.0);
    }

    private double ScoreForTaskType(TaskType taskType, ILLMProvider provider)
    {
        var providerName = provider.Name.ToLowerInvariant();
        var modelName = provider.GetModelName().ToLowerInvariant();

        return taskType switch
        {
            TaskType.Planning when providerName.Contains("azure") || modelName.Contains("gpt-5") => 1.0,
            TaskType.Planning when providerName.Contains("azure") || modelName.Contains("gpt-4") => 0.9,
            
            TaskType.CodeGeneration when modelName.Contains("qwen") => 1.0,
            TaskType.CodeGeneration when modelName.Contains("codellama") => 0.9,
            TaskType.CodeGeneration when modelName.Contains("deepseek") => 0.95,
            
            TaskType.Reasoning when providerName.Contains("azure") || modelName.Contains("gpt") => 1.0,
            TaskType.Reasoning when modelName.Contains("qwen2.5:32b") => 0.9,
            
            TaskType.Healing when providerName.Contains("azure") => 0.95,
            TaskType.Healing when modelName.Contains("qwen") => 0.85,
            
            TaskType.Extraction when modelName.Contains("llama") => 0.8,
            TaskType.Extraction when modelName.Contains("mistral") => 0.85,
            
            TaskType.Understanding => 0.7, // Most models can handle this
            TaskType.General => 0.6, // Fallback for any provider
            
            _ => 0.5 // Default score
        };
    }

    private double ScoreForComplexity(ComplexityLevel complexity, ProviderCapabilities capabilities)
    {
        return complexity switch
        {
            ComplexityLevel.Low => 0.5, // Any model works
            ComplexityLevel.Medium when capabilities.MaxContextTokens >= 8192 => 0.8,
            ComplexityLevel.Medium => 0.6,
            ComplexityLevel.High when capabilities.MaxContextTokens >= 16384 => 1.0,
            ComplexityLevel.High when capabilities.MaxContextTokens >= 8192 => 0.7,
            ComplexityLevel.Expert when capabilities.MaxContextTokens >= 32768 => 1.0,
            ComplexityLevel.Expert when capabilities.MaxContextTokens >= 16384 => 0.8,
            ComplexityLevel.Expert => 0.4,
            _ => 0.5
        };
    }

    private double ScoreForPriority(RequestPriority priority, ILLMProvider provider)
    {
        var providerName = provider.Name.ToLowerInvariant();

        return priority switch
        {
            RequestPriority.Critical when providerName.Contains("azure") => 1.0,
            RequestPriority.Critical => 0.6,
            
            RequestPriority.High when providerName.Contains("azure") => 0.9,
            RequestPriority.High => 0.7,
            
            RequestPriority.Normal => 0.8,
            RequestPriority.Low when providerName.Contains("ollama") => 1.0, // Prefer free/local for low priority
            RequestPriority.Low => 0.6,
            
            _ => 0.7
        };
    }
}

/// <summary>
/// Routing strategy that prefers cost-effective providers while maintaining quality.
/// </summary>
/// <remarks>
/// <para>
/// This strategy prioritizes:
/// 1. Prefer free/local providers (Ollama) for simple tasks
/// 2. Smaller models when complexity allows
/// 3. Azure OpenAI only for complex or high-priority tasks
/// </para>
/// <para>
/// Use this strategy to minimize LLM API costs while still providing good results.
/// </para>
/// </remarks>
public sealed class CostOptimizedRoutingStrategy : IRoutingStrategy
{
    /// <inheritdoc/>
    public string Name => "Cost-Optimized Routing";

    /// <inheritdoc/>
    public Task<ILLMProvider?> SelectProviderAsync(
        RoutingContext context,
        IReadOnlyList<ILLMProvider> availableProviders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(availableProviders);

        if (availableProviders.Count == 0)
        {
            return Task.FromResult<ILLMProvider?>(null);
        }

        var scores = availableProviders
            .Select(p => new { Provider = p, Score = ScoreProvider(context, p) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        return Task.FromResult(scores.FirstOrDefault()?.Provider);
    }

    /// <inheritdoc/>
    public double ScoreProvider(RoutingContext context, ILLMProvider provider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(provider);

        var capabilities = provider.GetCapabilities();

        // Check required capabilities
        if (context.RequiresStreaming && !capabilities.SupportsStreaming)
        {
            return 0.0;
        }

        if (context.RequiresFunctionCalling && !capabilities.SupportsFunctionCalling)
        {
            return 0.0;
        }

        var providerName = provider.Name.ToLowerInvariant();
        var score = 0.0;

        // Heavily prefer free/local providers
        if (providerName.Contains("ollama") || providerName.Contains("local"))
        {
            score += 0.9;
        }
        else
        {
            score += 0.3; // Azure OpenAI gets lower base score
        }

        // Adjust for complexity
        if (context.Complexity == ComplexityLevel.Low || context.Complexity == ComplexityLevel.Medium)
        {
            // Prefer local models for simple tasks
            if (providerName.Contains("ollama"))
            {
                score += 0.5;
            }
        }
        else
        {
            // Use Azure for complex tasks (quality over cost)
            if (providerName.Contains("azure"))
            {
                score += 0.7;
            }
        }

        // Adjust for priority
        if (context.Priority == RequestPriority.Critical && providerName.Contains("azure"))
        {
            // Reliability trumps cost
            score += 0.5;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }
}
