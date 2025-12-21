namespace EvoAITest.LLM.Models;

/// <summary>
/// Represents a request to an LLM provider.
/// </summary>
public sealed class LLMRequest
{
    /// <summary>
    /// Gets or sets the model to use for completion.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation messages.
    /// </summary>
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum tokens to generate.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the temperature (0-2, controls randomness).
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the top-p sampling value.
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Gets or sets the frequency penalty (-2.0 to 2.0).
    /// </summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// Gets or sets the presence penalty (-2.0 to 2.0).
    /// </summary>
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// Gets or sets stop sequences.
    /// </summary>
    public List<string>? Stop { get; set; }

    /// <summary>
    /// Gets or sets functions available for the model to call.
    /// </summary>
    public List<FunctionDefinition>? Functions { get; set; }

    /// <summary>
    /// Gets or sets the function call behavior.
    /// </summary>
    public object? FunctionCall { get; set; }

    /// <summary>
    /// Gets or sets whether to stream the response.
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// Gets or sets the user ID for tracking.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the response format.
    /// </summary>
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Gets or sets the random seed for deterministic results.
    /// </summary>
    public int? Seed { get; set; }
}

/// <summary>
/// Represents a message in a conversation.
/// </summary>
public sealed class Message
{
    /// <summary>
    /// Gets or sets the role of the message sender.
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the message sender (optional).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets function call information.
    /// </summary>
    public FunctionCall? FunctionCall { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the image URL for vision requests (base64 data URL).
    /// </summary>
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Defines message roles in a conversation.
/// </summary>
public enum MessageRole
{
    /// <summary>System instructions.</summary>
    System,
    
    /// <summary>User input.</summary>
    User,
    
    /// <summary>Assistant response.</summary>
    Assistant,
    
    /// <summary>Function result.</summary>
    Function
}

/// <summary>
/// Represents a function that can be called by the LLM.
/// </summary>
public sealed class FunctionDefinition
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function parameters (JSON Schema).
    /// </summary>
    public object? Parameters { get; set; }
}

/// <summary>
/// Represents a function call made by the LLM.
/// </summary>
public sealed class FunctionCall
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function arguments (JSON string).
    /// </summary>
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Defines the response format.
/// </summary>
public sealed class ResponseFormat
{
    /// <summary>
    /// Gets or sets the format type (e.g., "json_object", "text").
    /// </summary>
    public string Type { get; set; } = "text";
}
