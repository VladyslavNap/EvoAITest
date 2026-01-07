using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using EvoAITest.Core.Models;

namespace EvoAITest.Tests.LLM;

/// <summary>
/// Mock LLM provider for testing purposes.
/// </summary>
/// <remarks>
/// This mock allows simulating success, failures, latency, and streaming behavior
/// for testing routing, circuit breaker, and other LLM provider functionality.
/// </remarks>
public sealed class MockLLMProvider : ILLMProvider
{
    private readonly string _name;
    private readonly string _model;
    private bool _isAvailable = true;
    private int _callCount = 0;
    private readonly List<LLMRequest> _requests = new();
    private Func<LLMRequest, LLMResponse>? _responseFactory;
    private Func<LLMRequest, IAsyncEnumerable<LLMStreamChunk>>? _streamFactory;
    private TimeSpan _latency = TimeSpan.Zero;
    private int _failUntilCall = 0;
    private Exception? _exceptionToThrow;
    private TokenUsage _lastTokenUsage = new TokenUsage(0, 0, 0);

    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the supported model identifiers.
    /// </summary>
    public IReadOnlyList<string> SupportedModels => new List<string> { _model };

    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    public string Model => _model;

    /// <summary>
    /// Gets the number of times this provider has been called.
    /// </summary>
    public int CallCount => _callCount;

    /// <summary>
    /// Gets all requests made to this provider.
    /// </summary>
    public IReadOnlyList<LLMRequest> Requests => _requests.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="MockLLMProvider"/> class.
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <param name="model">The model identifier.</param>
    public MockLLMProvider(string name, string model)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Sets the availability of this provider.
    /// </summary>
    public MockLLMProvider WithAvailability(bool isAvailable)
    {
        _isAvailable = isAvailable;
        return this;
    }

    /// <summary>
    /// Sets a custom response factory.
    /// </summary>
    public MockLLMProvider WithResponseFactory(Func<LLMRequest, LLMResponse> factory)
    {
        _responseFactory = factory;
        return this;
    }

    /// <summary>
    /// Sets a custom stream factory.
    /// </summary>
    public MockLLMProvider WithStreamFactory(Func<LLMRequest, IAsyncEnumerable<LLMStreamChunk>> factory)
    {
        _streamFactory = factory;
        return this;
    }

    /// <summary>
    /// Simulates latency for all operations.
    /// </summary>
    public MockLLMProvider WithLatency(TimeSpan latency)
    {
        _latency = latency;
        return this;
    }

    /// <summary>
    /// Simulates failures until a specific call count.
    /// </summary>
    public MockLLMProvider FailUntilCall(int callNumber)
    {
        _failUntilCall = callNumber;
        return this;
    }

    /// <summary>
    /// Throws a specific exception on calls.
    /// </summary>
    public MockLLMProvider ThrowException(Exception exception)
    {
        _exceptionToThrow = exception;
        return this;
    }

    /// <summary>
    /// Resets the mock state.
    /// </summary>
    public void Reset()
    {
        _callCount = 0;
        _requests.Clear();
        _isAvailable = true;
        _responseFactory = null;
        _streamFactory = null;
        _latency = TimeSpan.Zero;
        _failUntilCall = 0;
        _exceptionToThrow = null;
        _lastTokenUsage = new TokenUsage(0, 0, 0);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(
        string prompt,
        Dictionary<string, string>? variables = null,
        List<BrowserTool>? tools = null,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default)
    {
        _callCount++;

        if (_latency > TimeSpan.Zero)
        {
            await Task.Delay(_latency, cancellationToken);
        }

        if (_callCount <= _failUntilCall || _exceptionToThrow != null)
        {
            throw _exceptionToThrow ?? new InvalidOperationException($"Mock failure for call {_callCount}");
        }

        _lastTokenUsage = new TokenUsage(10, 20, 0);
        return $"Response from {_name} for: {prompt}";
    }

    /// <inheritdoc/>
    public async Task<LLMResponse> CompleteAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _callCount++;
        _requests.Add(request);

        if (_latency > TimeSpan.Zero)
        {
            await Task.Delay(_latency, cancellationToken);
        }

        if (_callCount <= _failUntilCall || _exceptionToThrow != null)
        {
            throw _exceptionToThrow ?? new InvalidOperationException($"Mock failure for call {_callCount}");
        }

        if (_responseFactory != null)
        {
            return _responseFactory(request);
        }

        // Default response
        var content = $"Mock response from {_name} ({_model})";
        _lastTokenUsage = new TokenUsage(10, 20, 0m);
        
        return new LLMResponse
        {
            Id = Guid.NewGuid().ToString(),
            Model = _model,
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = content
                    },
                    FinishReason = "stop"
                }
            },
            Usage = new Usage
            {
                PromptTokens = 10,
                CompletionTokens = 20,
                TotalTokens = 30
            }
        };
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(
        LLMRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _callCount++;
        _requests.Add(request);

        if (_latency > TimeSpan.Zero)
        {
            await Task.Delay(_latency, cancellationToken);
        }

        if (_callCount <= _failUntilCall || _exceptionToThrow != null)
        {
            throw _exceptionToThrow ?? new InvalidOperationException($"Mock failure for call {_callCount}");
        }

        if (_streamFactory != null)
        {
            await foreach (var chunk in _streamFactory(request).WithCancellation(cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        // Default streaming behavior
        var chunkId = Guid.NewGuid().ToString();
        var words = $"Mock stream from {_name}".Split(' ');

        foreach (var word in words)
        {
            yield return new LLMStreamChunk
            {
                Id = chunkId,
                Delta = word + " ",
                FinishReason = null
            };

            if (_latency > TimeSpan.Zero)
            {
                await Task.Delay(_latency / words.Length, cancellationToken);
            }
        }

        yield return new LLMStreamChunk
        {
            Id = chunkId,
            Delta = string.Empty,
            FinishReason = "stop"
        };

        _lastTokenUsage = new TokenUsage(10, 20, 0);
    }

    /// <inheritdoc/>
    public Task<float[]> GenerateEmbeddingAsync(
        string text,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        _callCount++;

        // Return mock embedding vector
        var embedding = new float[1536]; // Standard OpenAI embedding size
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)Math.Sin(i * 0.1);
        }

        return Task.FromResult(embedding);
    }

    /// <inheritdoc/>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_isAvailable);
    }

    /// <inheritdoc/>
    public ProviderCapabilities GetCapabilities()
    {
        return new ProviderCapabilities
        {
            SupportsStreaming = true,
            SupportsFunctionCalling = true,
            SupportsVision = false,
            MaxContextTokens = 128000,
            MaxOutputTokens = 4096
        };
    }

    /// <inheritdoc/>
    public Task<List<ToolCall>> ParseToolCallsAsync(
        string response,
        CancellationToken cancellationToken = default)
    {
        // Return empty list for mock
        return Task.FromResult(new List<ToolCall>());
    }

    /// <inheritdoc/>
    public string GetModelName() => _model;

    /// <inheritdoc/>
    public TokenUsage GetLastTokenUsage() => _lastTokenUsage;

    /// <summary>
    /// Creates a mock provider that always succeeds.
    /// </summary>
    public static MockLLMProvider CreateSuccessful(string name = "MockProvider", string model = "mock-model")
    {
        return new MockLLMProvider(name, model);
    }

    /// <summary>
    /// Creates a mock provider that always fails.
    /// </summary>
    public static MockLLMProvider CreateFailing(string name = "FailingProvider", string model = "mock-model")
    {
        return new MockLLMProvider(name, model)
            .ThrowException(new InvalidOperationException("Mock provider failure"));
    }

    /// <summary>
    /// Creates a mock provider with high latency.
    /// </summary>
    public static MockLLMProvider CreateSlow(TimeSpan latency, string name = "SlowProvider", string model = "mock-model")
    {
        return new MockLLMProvider(name, model)
            .WithLatency(latency);
    }
}
