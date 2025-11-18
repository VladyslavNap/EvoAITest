namespace EvoAITest.LLM.Providers;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using EvoAITest.Core.Models;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

/// <summary>
/// Azure OpenAI provider implementation using the Azure.AI.OpenAI 2.0+ SDK.
/// Supports chat completions, streaming, function calling, and embeddings.
/// </summary>
public sealed class AzureOpenAIProvider : ILLMProvider
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly ChatClient _chatClient;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAIProvider> _logger;
    private TokenUsage _lastUsage;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAIProvider"/> class using API key authentication.
    /// </summary>
    /// <param name="endpoint">Azure OpenAI endpoint URL (e.g., https://your-resource.openai.azure.com).</param>
    /// <param name="apiKey">Azure OpenAI API key (should be retrieved from Key Vault in production).</param>
    /// <param name="deploymentName">The deployment name for the model (e.g., gpt-4, gpt-5).</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public AzureOpenAIProvider(
        string endpoint,
        string apiKey,
        string deploymentName,
        ILogger<AzureOpenAIProvider> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName, nameof(deploymentName));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _deploymentName = deploymentName;
        _logger = logger;
        _lastUsage = new TokenUsage(0, 0, 0);

        _logger.LogInformation("Initializing Azure OpenAI provider with endpoint: {Endpoint}, deployment: {Deployment}",
            endpoint, deploymentName);

        _azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));

        _chatClient = _azureClient.GetChatClient(deploymentName);

        _logger.LogInformation("Azure OpenAI provider initialized successfully");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAIProvider"/> class using Microsoft Entra ID authentication.
    /// </summary>
    /// <param name="endpoint">Azure OpenAI endpoint URL.</param>
    /// <param name="deploymentName">The deployment name for the model.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <remarks>
    /// This constructor uses DefaultAzureCredential for keyless authentication.
    /// Ensure the application has the appropriate Azure RBAC role assignment.
    /// </remarks>
    public AzureOpenAIProvider(
        string endpoint,
        string deploymentName,
        ILogger<AzureOpenAIProvider> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName, nameof(deploymentName));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _deploymentName = deploymentName;
        _logger = logger;
        _lastUsage = new TokenUsage(0, 0, 0);

        _logger.LogInformation("Initializing Azure OpenAI provider with Managed Identity. Endpoint: {Endpoint}, Deployment: {Deployment}",
            endpoint, deploymentName);

        _azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new DefaultAzureCredential());

        _chatClient = _azureClient.GetChatClient(deploymentName);

        _logger.LogInformation("Azure OpenAI provider initialized successfully with Managed Identity");
    }

    /// <inheritdoc/>
    public string Name => "Azure OpenAI";

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedModels => new[]
    {
        "gpt-4",
        "gpt-4-turbo",
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-5",
        "gpt-3.5-turbo",
        "o1",
        "o1-mini",
        "o3-mini"
    };

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(
        string prompt,
        Dictionary<string, string>? variables = null,
        List<BrowserTool>? tools = null,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));

        var finalPrompt = SubstituteVariables(prompt, variables);

        _logger.LogDebug("Generating completion with prompt length: {Length}, maxTokens: {MaxTokens}",
            finalPrompt.Length, maxTokens);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are an AI browser automation assistant. Analyze web pages and generate precise automation steps using the provided tools."),
            new UserChatMessage(finalPrompt)
        };

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxTokens,
            Temperature = 0.7f
        };

        // Add tools if provided
        if (tools is not null && tools.Count > 0)
        {
            ConvertBrowserToolsToChatTools(tools, options);
        }

        try
        {
            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            // Track token usage
            if (completion.Value.Usage is not null)
            {
                var usage = completion.Value.Usage;
                var cost = CalculateCost(usage.InputTokenCount, usage.OutputTokenCount);
                _lastUsage = new TokenUsage(usage.InputTokenCount, usage.OutputTokenCount, cost);

                _logger.LogDebug("Completion successful. Input tokens: {Input}, Output tokens: {Output}, Cost: ${Cost:F4}",
                    usage.InputTokenCount, usage.OutputTokenCount, cost);
            }

            return completion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate completion from Azure OpenAI");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ToolCall>> ParseToolCallsAsync(
        string response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(response, nameof(response));

        var toolCalls = new List<ToolCall>();

        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            // Try to parse as array of tool calls
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    var toolCall = ParseSingleToolCall(element);
                    if (toolCall is not null)
                    {
                        toolCalls.Add(toolCall);
                    }
                }
            }
            // Try to parse as single tool call
            else if (root.ValueKind == JsonValueKind.Object)
            {
                var toolCall = ParseSingleToolCall(root);
                if (toolCall is not null)
                {
                    toolCalls.Add(toolCall);
                }
            }

            _logger.LogDebug("Parsed {Count} tool calls from response", toolCalls.Count);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse tool calls from response. Response may not be valid JSON.");
        }

        return toolCalls;
    }

    /// <inheritdoc/>
    public string GetModelName() => _deploymentName;

    /// <inheritdoc/>
    public TokenUsage GetLastTokenUsage() => _lastUsage;

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Azure OpenAI availability");

            var testMessages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a test assistant."),
                new UserChatMessage("Respond with 'OK' if you can receive this message.")
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 10
            };

            var completion = await _chatClient.CompleteChatAsync(testMessages, options, cancellationToken);

            var isAvailable = !string.IsNullOrWhiteSpace(completion.Value.Content[0].Text);

            _logger.LogInformation("Azure OpenAI availability check: {Status}", isAvailable ? "Available" : "Unavailable");

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure OpenAI availability check failed");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug("Processing LLM request with {MessageCount} messages", request.Messages.Count);

        var chatMessages = ConvertToOpenAIChatMessages(request.Messages);
        var options = BuildChatCompletionOptions(request);

        try
        {
            var completion = await _chatClient.CompleteChatAsync(chatMessages, options, cancellationToken);

            // Track usage
            if (completion.Value.Usage is not null)
            {
                var usage = completion.Value.Usage;
                var cost = CalculateCost(usage.InputTokenCount, usage.OutputTokenCount);
                _lastUsage = new TokenUsage(usage.InputTokenCount, usage.OutputTokenCount, cost);
            }

            return ConvertToLLMResponse(completion.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete LLM request");
            throw;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug("Starting streaming completion with {MessageCount} messages", request.Messages.Count);

        var chatMessages = ConvertToOpenAIChatMessages(request.Messages);
        var options = BuildChatCompletionOptions(request);

        var streamingUpdates = _chatClient.CompleteChatStreamingAsync(chatMessages, options, cancellationToken);
        var chunkId = Guid.NewGuid().ToString();

        await foreach (var update in streamingUpdates.WithCancellation(cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return new LLMStreamChunk
                    {
                        Id = chunkId,
                        Delta = contentPart.Text,
                        FinishReason = update.FinishReason?.ToString()
                    };
                }
            }

            // Track usage from the final update
            if (update.Usage is not null)
            {
                var usage = update.Usage;
                var cost = CalculateCost(usage.InputTokenCount, usage.OutputTokenCount);
                _lastUsage = new TokenUsage(usage.InputTokenCount, usage.OutputTokenCount, cost);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        _logger.LogDebug("Generating embedding for text length: {Length}", text.Length);

        var embeddingModel = model ?? "text-embedding-3-small";
        var embeddingClient = _azureClient.GetEmbeddingClient(embeddingModel);

        try
        {
            var embedding = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
            return embedding.Value.ToFloats().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding");
            throw;
        }
    }

    /// <inheritdoc/>
    public ProviderCapabilities GetCapabilities()
    {
        return new ProviderCapabilities
        {
            SupportsStreaming = true,
            SupportsFunctionCalling = true,
            SupportsVision = true,
            SupportsEmbeddings = true,
            MaxContextTokens = 128000, // GPT-4/GPT-5 context window
            MaxOutputTokens = 16384    // GPT-4 max output tokens
        };
    }

    // ============================================================
    // Private Helper Methods
    // ============================================================

    private string SubstituteVariables(string prompt, Dictionary<string, string>? variables)
    {
        if (variables is null || variables.Count == 0)
        {
            return prompt;
        }

        var result = prompt;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{key}}}", value);
        }

        return result;
    }

    private void ConvertBrowserToolsToChatTools(List<BrowserTool> browserTools, ChatCompletionOptions options)
    {
        foreach (var browserTool in browserTools)
        {
            try
            {
                var functionDefinition = ChatTool.CreateFunctionTool(
                    functionName: browserTool.Name,
                    functionDescription: browserTool.Description,
                    functionParameters: BinaryData.FromObjectAsJson(browserTool.Parameters)
                );

                options.Tools.Add(functionDefinition);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert browser tool: {ToolName}", browserTool.Name);
            }
        }
    }

    private ToolCall? ParseSingleToolCall(JsonElement element)
    {
        try
        {
            if (!element.TryGetProperty("tool_name", out var toolNameElement) &&
                !element.TryGetProperty("name", out toolNameElement))
            {
                return null;
            }

            var toolName = toolNameElement.GetString() ?? string.Empty;

            var parameters = new Dictionary<string, object>();
            if (element.TryGetProperty("parameters", out var paramsElement) ||
                element.TryGetProperty("arguments", out paramsElement))
            {
                foreach (var param in paramsElement.EnumerateObject())
                {
                    parameters[param.Name] = param.Value.ValueKind switch
                    {
                        JsonValueKind.String => param.Value.GetString() ?? string.Empty,
                        JsonValueKind.Number => param.Value.GetInt32(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => param.Value.ToString()
                    };
                }
            }

            var reasoning = element.TryGetProperty("reasoning", out var reasoningElement)
                ? reasoningElement.GetString() ?? string.Empty
                : string.Empty;

            var correlationId = element.TryGetProperty("correlation_id", out var corrElement)
                ? corrElement.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString();

            return new ToolCall(toolName, parameters, reasoning, correlationId);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse individual tool call: Invalid JSON format");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to parse individual tool call: Invalid operation on JSON element");
            return null;
        }
    }

    private List<ChatMessage> ConvertToOpenAIChatMessages(List<Message> messages)
    {
        var chatMessages = new List<ChatMessage>();

        foreach (var message in messages)
        {
            chatMessages.Add(message.Role switch
            {
                MessageRole.System => new SystemChatMessage(message.Content),
                MessageRole.User => new UserChatMessage(message.Content),
                MessageRole.Assistant => new AssistantChatMessage(message.Content),
                _ => new UserChatMessage(message.Content)
            });
        }

        return chatMessages;
    }

    private ChatCompletionOptions BuildChatCompletionOptions(LLMRequest request)
    {
        var options = new ChatCompletionOptions();

        if (request.MaxTokens.HasValue)
        {
            options.MaxOutputTokenCount = request.MaxTokens.Value;
        }

        if (request.Temperature.HasValue)
        {
            options.Temperature = (float)request.Temperature.Value;
        }

        if (request.TopP.HasValue)
        {
            options.TopP = (float)request.TopP.Value;
        }

        if (request.FrequencyPenalty.HasValue)
        {
            options.FrequencyPenalty = (float)request.FrequencyPenalty.Value;
        }

        if (request.PresencePenalty.HasValue)
        {
            options.PresencePenalty = (float)request.PresencePenalty.Value;
        }

        if (request.Stop is not null)
        {
            foreach (var stop in request.Stop)
            {
                options.StopSequences.Add(stop);
            }
        }

        return options;
    }

    private LLMResponse ConvertToLLMResponse(ChatCompletion completion)
    {
        var response = new LLMResponse
        {
            Id = completion.Id,
            Model = completion.Model,
            CreatedAt = DateTimeOffset.UtcNow,
            FinishReason = completion.FinishReason.ToString()
        };

        var choice = new Choice
        {
            Index = 0,
            Message = new Message
            {
                Role = MessageRole.Assistant,
                Content = completion.Content[0].Text,
                Timestamp = DateTimeOffset.UtcNow
            },
            FinishReason = completion.FinishReason.ToString()
        };

        response.Choices.Add(choice);

        if (completion.Usage is not null)
        {
            response.Usage = new Usage
            {
                PromptTokens = completion.Usage.InputTokenCount,
                CompletionTokens = completion.Usage.OutputTokenCount,
                TotalTokens = completion.Usage.TotalTokenCount
            };
        }

        return response;
    }

    private decimal CalculateCost(int inputTokens, int outputTokens)
    {
        // GPT-4/GPT-5 pricing (adjust based on actual deployment)
        // These are example rates - update with actual Azure OpenAI pricing
        const decimal inputCostPer1k = 0.03m;    // $0.03 per 1K input tokens
        const decimal outputCostPer1k = 0.06m;   // $0.06 per 1K output tokens

        var inputCost = (inputTokens / 1000m) * inputCostPer1k;
        var outputCost = (outputTokens / 1000m) * outputCostPer1k;

        return inputCost + outputCost;
    }
}
