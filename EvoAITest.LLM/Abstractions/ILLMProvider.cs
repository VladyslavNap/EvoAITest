using EvoAITest.Core.Models;
using EvoAITest.LLM.Models;

namespace EvoAITest.LLM.Abstractions;

/// <summary>
/// Represents token usage information for an LLM request (immutable).
/// </summary>
/// <param name="InputTokens">Number of tokens in the input/prompt.</param>
/// <param name="OutputTokens">Number of tokens in the generated output.</param>
/// <param name="EstimatedCostUSD">Estimated cost in USD for this request.</param>
public sealed record TokenUsage(
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCostUSD
)
{
    /// <summary>
    /// Gets the total number of tokens (input + output).
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
}

/// <summary>
/// Defines the core contract for Large Language Model providers.
/// This interface provides high-level methods for generating completions,
/// parsing tool calls, and tracking token usage.
/// </summary>
/// <remarks>
/// Implementations wrap LLM APIs (OpenAI, Azure OpenAI, Anthropic, etc.)
/// and handle provider-specific details like authentication, rate limiting,
/// and response parsing.
/// </remarks>
public interface ILLMProvider
{
    /// <summary>
    /// Gets the name of this LLM provider (e.g., "OpenAI", "Azure OpenAI", "Anthropic").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the supported model identifiers for this provider.
    /// </summary>
    /// <remarks>
    /// For example, OpenAI might return ["gpt-4", "gpt-3.5-turbo"],
    /// while Anthropic might return ["claude-3-opus", "claude-3-sonnet"].
    /// </remarks>
    IReadOnlyList<string> SupportedModels { get; }

    /// <summary>
    /// Generates a completion from the LLM based on the provided prompt and optional tools.
    /// </summary>
    /// <param name="prompt">
    /// The prompt template to send to the LLM. Can include variables in the format {variableName}.
    /// </param>
    /// <param name="variables">
    /// Optional dictionary of variable names and values to substitute into the prompt.
    /// </param>
    /// <param name="tools">
    /// Optional list of browser automation tools that the LLM can call.
    /// When provided, the LLM may respond with tool calls instead of plain text.
    /// </param>
    /// <param name="maxTokens">
    /// Maximum number of tokens to generate in the response. Default is 2000.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the generated text response from the LLM.
    /// If tools are provided and the LLM decides to call them, the response
    /// will contain the tool calls in a structured format.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method handles prompt variable substitution automatically.
    /// For example, if prompt = "Navigate to {url}" and variables = {"url": "https://example.com"},
    /// the actual prompt sent will be "Navigate to https://example.com".
    /// </para>
    /// <para>
    /// When tools are provided, the LLM may choose to respond with tool calls
    /// instead of plain text. Use <see cref="ParseToolCallsAsync"/> to extract
    /// the tool calls from the response.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="prompt"/> is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the provider is not properly configured or authenticated.
    /// </exception>
    Task<string> GenerateAsync(
        string prompt,
        Dictionary<string, string>? variables = null,
        List<BrowserTool>? tools = null,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses tool calls from an LLM response.
    /// </summary>
    /// <param name="response">
    /// The raw response string from the LLM that may contain tool calls.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a list of parsed tool calls.
    /// Returns an empty list if no tool calls are found in the response.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method extracts structured tool calls from the LLM response,
    /// handling provider-specific formats (function calling, JSON mode, etc.).
    /// </para>
    /// <para>
    /// Each <see cref="ToolCall"/> includes the tool name,
    /// parameters, reasoning, and a correlation ID for tracing.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="response"/> is null.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if the response contains malformed tool call data.
    /// </exception>
    Task<List<ToolCall>> ParseToolCallsAsync(
        string response,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name/identifier of the model currently in use.
    /// </summary>
    /// <returns>
    /// The model identifier (e.g., "gpt-4", "claude-3-opus-20240229").
    /// </returns>
    /// <remarks>
    /// This is useful for logging, debugging, and cost tracking purposes.
    /// The returned value should match one of the identifiers in <see cref="SupportedModels"/>.
    /// </remarks>
    string GetModelName();

    /// <summary>
    /// Gets the token usage information from the most recent LLM request.
    /// </summary>
    /// <returns>
    /// A <see cref="TokenUsage"/> record containing input tokens, output tokens,
    /// total tokens, and estimated cost. Returns a zero-valued record if no
    /// requests have been made yet.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Token usage is important for cost tracking and rate limit monitoring.
    /// The estimated cost is calculated based on the provider's pricing model.
    /// </para>
    /// <para>
    /// Note: This returns only the LAST request's usage. For multi-request scenarios,
    /// implement your own aggregation or use OpenTelemetry metrics.
    /// </para>
    /// </remarks>
    TokenUsage GetLastTokenUsage();

    /// <summary>
    /// Checks if the LLM provider is currently available and can accept requests.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the availability check.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is <c>true</c> if the provider is available and ready;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a health check on the LLM provider, verifying:
    /// - API credentials are valid
    /// - The service is reachable
    /// - Rate limits are not exceeded
    /// - The configured model is available
    /// </para>
    /// <para>
    /// Use this method before starting long-running automation tasks to ensure
    /// the LLM provider is ready. This is particularly useful in containerized
    /// environments where network conditions may vary.
    /// </para>
    /// </remarks>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a completion request to the LLM.
    /// </summary>
    /// <param name="request">The completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The LLM response.</returns>
    /// <remarks>
    /// This is a lower-level method that provides full control over the request.
    /// For most use cases, prefer using <see cref="GenerateAsync"/> instead.
    /// </remarks>
    Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a completion response from the LLM.
    /// </summary>
    /// <param name="request">The completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response chunks.</returns>
    /// <remarks>
    /// Use streaming for real-time user feedback or when processing long responses.
    /// </remarks>
    IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="model">Optional embedding model to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding vector.</returns>
    /// <remarks>
    /// Embeddings are useful for semantic search, similarity comparison,
    /// and other vector-based operations.
    /// </remarks>
    Task<float[]> GenerateEmbeddingAsync(string text, string? model = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the provider's capabilities.
    /// </summary>
    /// <returns>Provider capabilities.</returns>
    /// <remarks>
    /// Use this to determine what features are supported before making requests.
    /// </remarks>
    ProviderCapabilities GetCapabilities();
}

/// <summary>
/// Defines capabilities of an LLM provider.
/// </summary>
public sealed class ProviderCapabilities
{
    /// <summary>
    /// Gets or sets a value indicating whether the provider supports streaming.
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider supports function calling.
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider supports vision inputs.
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider supports embeddings.
    /// </summary>
    public bool SupportsEmbeddings { get; set; }

    /// <summary>
    /// Gets or sets the maximum context window size in tokens.
    /// </summary>
    public int MaxContextTokens { get; set; }

    /// <summary>
    /// Gets or sets the maximum output tokens per request.
    /// </summary>
    public int MaxOutputTokens { get; set; }
}
