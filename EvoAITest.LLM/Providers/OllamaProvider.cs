namespace EvoAITest.LLM.Providers;

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using EvoAITest.Core.Models;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Ollama provider implementation for local LLM inference.
/// Supports chat completions, streaming, and basic tool calling with open-source models.
/// </summary>
/// <remarks>
/// Ollama must be installed and running locally. Install from: https://ollama.ai
/// Popular models: qwen2.5:32b, qwen2.5-7b, llama3, mistral, codellama, phi, gemma
/// </remarks>
public sealed class OllamaProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly ILogger<OllamaProvider> _logger;
    private TokenUsage _lastUsage;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProvider"/> class.
    /// </summary>
    /// <param name="baseUrl">Ollama API base URL (e.g., http://localhost:11434).</param>
    /// <param name="model">Model name (e.g., qwen2.5-7b, llama2, mistral).</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public OllamaProvider(string baseUrl, string model, ILogger<OllamaProvider> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl, nameof(baseUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _logger = logger;
        _lastUsage = new TokenUsage(0, 0, 0);

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5) // Ollama can be slow for large contexts
        };

        _logger.LogInformation("Initializing Ollama provider with endpoint: {Endpoint}, model: {Model}",
            _baseUrl, _model);
    }

    /// <inheritdoc/>
    public string Name => "Ollama";

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedModels => new[]
    {
        "qwen2.5:32b",
        "qwen2.5-7b",
        "llama2",
        "llama3",
        "mistral",
        "mixtral",
        "codellama",
        "phi",
        "gemma",
        "neural-chat",
        "starling-lm",
        "vicuna",
        "orca-mini"
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

        // Add tool instructions if tools are provided
        if (tools is not null && tools.Count > 0)
        {
            finalPrompt = BuildPromptWithTools(finalPrompt, tools);
        }

        _logger.LogDebug("Generating completion with prompt length: {Length}, maxTokens: {MaxTokens}",
            finalPrompt.Length, maxTokens);

        var request = new OllamaGenerateRequest
        {
            Model = _model,
            Prompt = finalPrompt,
            Stream = false,
            Options = new OllamaOptions
            {
                NumPredict = maxTokens,
                Temperature = 0.7f,
                TopP = 0.9f
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/generate",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Ollama returned null response");

            // Estimate token usage (Ollama doesn't provide exact counts)
            var estimatedInputTokens = EstimateTokens(finalPrompt);
            var estimatedOutputTokens = EstimateTokens(result.Response);
            _lastUsage = new TokenUsage(estimatedInputTokens, estimatedOutputTokens, 0); // Ollama is free

            _logger.LogDebug("Completion successful. Estimated input tokens: {Input}, output tokens: {Output}",
                estimatedInputTokens, estimatedOutputTokens);

            return result.Response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to Ollama failed. Is Ollama running at {BaseUrl}?", _baseUrl);
            throw new InvalidOperationException($"Failed to connect to Ollama at {_baseUrl}. Ensure Ollama is running.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate completion from Ollama");
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
            // Try to parse JSON response for tool calls
            // Ollama models may output structured JSON if prompted correctly
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            // Check for tool_calls array format
            if (root.TryGetProperty("tool_calls", out var toolCallsElement) && toolCallsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in toolCallsElement.EnumerateArray())
                {
                    var toolCall = ParseSingleToolCall(element);
                    if (toolCall is not null)
                    {
                        toolCalls.Add(toolCall);
                    }
                }
            }
            // Check for single tool call
            else if (root.TryGetProperty("tool_name", out _) || root.TryGetProperty("name", out _))
            {
                var toolCall = ParseSingleToolCall(root);
                if (toolCall is not null)
                {
                    toolCalls.Add(toolCall);
                }
            }

            _logger.LogDebug("Parsed {Count} tool calls from Ollama response", toolCalls.Count);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse tool calls from Ollama response. Response may not contain valid JSON.");
        }

        return toolCalls;
    }

    /// <inheritdoc/>
    public string GetModelName() => _model;

    /// <inheritdoc/>
    public TokenUsage GetLastTokenUsage() => _lastUsage;

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Ollama availability at {BaseUrl}", _baseUrl);

            // Check if Ollama server is running
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama server responded with status: {StatusCode}", response.StatusCode);
                return false;
            }

            // Verify the model is available
            var tags = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken: cancellationToken);
            var modelExists = tags?.Models?.Any(m => m.Name == _model || m.Name.StartsWith(_model)) ?? false;

            if (!modelExists)
            {
                _logger.LogWarning("Model {Model} not found in Ollama. Available models: {Models}",
                    _model, string.Join(", ", tags?.Models?.Select(m => m.Name) ?? Array.Empty<string>()));
                return false;
            }

            _logger.LogInformation("Ollama is available with model: {Model}", _model);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ollama availability check failed due to HTTP error. Is Ollama running at {BaseUrl}?", _baseUrl);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Ollama availability check timed out. Is Ollama running at {BaseUrl}?", _baseUrl);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug("Processing LLM request with {MessageCount} messages", request.Messages.Count);

        // Convert messages to a single prompt
        var prompt = ConvertMessagesToPrompt(request.Messages);

        // Build Ollama request
        var ollamaRequest = new OllamaGenerateRequest
        {
            Model = request.Model ?? _model,
            Prompt = prompt,
            Stream = false,
            Options = BuildOllamaOptions(request)
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/generate",
                ollamaRequest,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Ollama returned null response");

            // Estimate token usage
            var estimatedInputTokens = EstimateTokens(prompt);
            var estimatedOutputTokens = EstimateTokens(ollamaResponse.Response);
            _lastUsage = new TokenUsage(estimatedInputTokens, estimatedOutputTokens, 0);

            return ConvertToLLMResponse(ollamaResponse, request.Model ?? _model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete LLM request with Ollama");
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

        var prompt = ConvertMessagesToPrompt(request.Messages);

        var ollamaRequest = new OllamaGenerateRequest
        {
            Model = request.Model ?? _model,
            Prompt = prompt,
            Stream = true,
            Options = BuildOllamaOptions(request)
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
        {
            Content = JsonContent.Create(ollamaRequest)
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var chunkId = Guid.NewGuid().ToString();
        var totalOutputTokens = 0;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            OllamaStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Ollama stream chunk: {Line}", line);
                continue;
            }

            if (chunk?.Response is not null && !string.IsNullOrEmpty(chunk.Response))
            {
                totalOutputTokens += EstimateTokens(chunk.Response);

                yield return new LLMStreamChunk
                {
                    Id = chunkId,
                    Delta = chunk.Response,
                    FinishReason = chunk.Done ? "stop" : null
                };
            }

            if (chunk?.Done == true)
            {
                // Update final token usage
                var inputTokens = EstimateTokens(prompt);
                _lastUsage = new TokenUsage(inputTokens, totalOutputTokens, 0);
                break;
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

        var embeddingModel = model ?? _model;

        var request = new OllamaEmbeddingRequest
        {
            Model = embeddingModel,
            Prompt = text
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/embeddings",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Ollama returned null embedding response");

            return result.Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding from Ollama");
            throw;
        }
    }

    /// <inheritdoc/>
    public ProviderCapabilities GetCapabilities()
    {
        return new ProviderCapabilities
        {
            SupportsStreaming = true,
            SupportsFunctionCalling = true, // Via prompt engineering
            SupportsVision = _model.Contains("llava") || _model.Contains("bakllava"), // Vision models
            SupportsEmbeddings = true,
            MaxContextTokens = GetModelContextWindow(_model),
            MaxOutputTokens = 2048 // Most Ollama models
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

    private string BuildPromptWithTools(string userPrompt, List<BrowserTool> tools)
    {
        var toolsJson = JsonSerializer.Serialize(tools, new JsonSerializerOptions { WriteIndented = true });

        return $$"""
            You are a browser automation assistant. You have access to the following tools:

            {{toolsJson}}

            User Request: {{userPrompt}}

            Respond with a JSON object containing a "tool_calls" array. Each tool call should have:
            - "tool_name": The name of the tool to use
            - "parameters": The parameters for the tool
            - "reasoning": Why you chose this tool and parameters

            Example response format:
            {
              "tool_calls": [
                {
                  "tool_name": "navigate",
                  "parameters": { "url": "https://example.com" },
                  "reasoning": "Navigate to the target website"
                }
              ]
            }
            """;
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
            _logger.LogWarning(ex, "Failed to parse individual tool call (JSON error)");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to parse individual tool call (invalid operation)");
            return null;
        }
    }

    private string ConvertMessagesToPrompt(List<Message> messages)
    {
        var promptBuilder = new System.Text.StringBuilder();

        foreach (var message in messages)
        {
            var rolePrefix = message.Role switch
            {
                MessageRole.System => "System",
                MessageRole.User => "User",
                MessageRole.Assistant => "Assistant",
                _ => "User"
            };

            promptBuilder.AppendLine($"{rolePrefix}: {message.Content}");
            promptBuilder.AppendLine();
        }

        return promptBuilder.ToString().TrimEnd();
    }

    private OllamaOptions BuildOllamaOptions(LLMRequest request)
    {
        return new OllamaOptions
        {
            NumPredict = request.MaxTokens ?? 2000,
            Temperature = request.Temperature.HasValue ? (float)request.Temperature.Value : 0.7f,
            TopP = request.TopP.HasValue ? (float)request.TopP.Value : 0.9f,
            FrequencyPenalty = request.FrequencyPenalty.HasValue ? (float)request.FrequencyPenalty.Value : 0f,
            PresencePenalty = request.PresencePenalty.HasValue ? (float)request.PresencePenalty.Value : 0f,
            Stop = request.Stop?.ToArray()
        };
    }

    private LLMResponse ConvertToLLMResponse(OllamaGenerateResponse ollamaResponse, string model)
    {
        var response = new LLMResponse
        {
            Id = Guid.NewGuid().ToString(),
            Model = model,
            CreatedAt = DateTimeOffset.UtcNow,
            FinishReason = "stop"
        };

        var choice = new Choice
        {
            Index = 0,
            Message = new Message
            {
                Role = MessageRole.Assistant,
                Content = ollamaResponse.Response,
                Timestamp = DateTimeOffset.UtcNow
            },
            FinishReason = "stop"
        };

        response.Choices.Add(choice);

        // Estimate token usage
        var inputTokens = _lastUsage.InputTokens;
        var outputTokens = _lastUsage.OutputTokens;

        response.Usage = new Usage
        {
            PromptTokens = inputTokens,
            CompletionTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens
        };

        return response;
    }

    private int EstimateTokens(string text)
    {
        // Rough estimation: ~4 characters per token for English text
        // This is an approximation since Ollama doesn't provide exact token counts
        return text.Length / 4;
    }

    private int GetModelContextWindow(string modelName)
    {
        // Return context window based on model name
        return modelName.ToLowerInvariant() switch
        {
            var m when m.Contains("qwen2.5:32b") || m.Contains("qwen2.5-32b") => 32768,
            var m when m.Contains("llama3") => 8192,
            var m when m.Contains("llama2") => 4096,
            var m when m.Contains("mistral") => 8192,
            var m when m.Contains("mixtral") => 32768,
            var m when m.Contains("qwen") => 8192,
            var m when m.Contains("codellama") => 16384,
            var m when m.Contains("phi") => 2048,
            var m when m.Contains("gemma") => 8192,
            _ => 4096 // Default
        };
    }

    // ============================================================
    // Ollama API Models
    // ============================================================

    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; set; }
    }

    private sealed class OllamaOptions
    {
        [JsonPropertyName("num_predict")]
        public int NumPredict { get; set; }

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public float TopP { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public float FrequencyPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public float PresencePenalty { get; set; }

        [JsonPropertyName("stop")]
        public string[]? Stop { get; set; }
    }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("context")]
        public int[]? Context { get; set; }
    }

    private sealed class OllamaStreamChunk
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private sealed class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    private sealed class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel>? Models { get; set; }
    }

    private sealed class OllamaModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("modified_at")]
        public DateTime ModifiedAt { get; set; }
    }
}
