namespace EvoAITest.LLM.Models;

/// <summary>
/// Represents a response from an LLM provider.
/// </summary>
public sealed class LLMResponse
{
    /// <summary>
    /// Gets or sets the unique response ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model that generated the response.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated choices.
    /// </summary>
    public List<Choice> Choices { get; set; } = new();

    /// <summary>
    /// Gets or sets usage statistics.
    /// </summary>
    public Usage? Usage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the finish reason.
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Gets the first choice's message content, or empty string if no choices.
    /// </summary>
    public string Content => Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
}

/// <summary>
/// Represents a completion choice.
/// </summary>
public sealed class Choice
{
    /// <summary>
    /// Gets or sets the index of this choice.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public Message? Message { get; set; }

    /// <summary>
    /// Gets or sets the finish reason.
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Gets or sets the log probabilities (if requested).
    /// </summary>
    public object? LogProbs { get; set; }
}

/// <summary>
/// Represents token usage statistics.
/// </summary>
public sealed class Usage
{
    /// <summary>
    /// Gets or sets the number of prompt tokens.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of completion tokens.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens.
    /// </summary>
    public int TotalTokens { get; set; }
}

/// <summary>
/// Represents a chunk in a streaming response.
/// </summary>
public sealed class LLMStreamChunk
{
    /// <summary>
    /// Gets or sets the chunk ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the chunk content delta.
    /// </summary>
    public string Delta { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the finish reason (if this is the last chunk).
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the last chunk.
    /// </summary>
    public bool IsComplete => !string.IsNullOrEmpty(FinishReason);
}
