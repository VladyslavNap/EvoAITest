using EvoAITest.Core.Models;
using EvoAITest.LLM.Models;
using EvoAITest.LLM.Prompts;

namespace EvoAITest.LLM.Abstractions;

/// <summary>
/// Defines a contract for building and managing LLM prompts with templates and injection protection.
/// </summary>
public interface IPromptBuilder
{
    #region Legacy Message Building (For Backward Compatibility)

    /// <summary>
    /// Builds a system prompt with the given content.
    /// </summary>
    /// <param name="content">The system prompt content.</param>
    /// <returns>A message representing the system prompt.</returns>
    Message BuildSystemPrompt(string content);

    /// <summary>
    /// Builds a user message with the given content.
    /// </summary>
    /// <param name="content">The user message content.</param>
    /// <returns>A message representing the user input.</returns>
    Message BuildUserMessage(string content);

    /// <summary>
    /// Builds an assistant message with the given content.
    /// </summary>
    /// <param name="content">The assistant message content.</param>
    /// <returns>A message representing the assistant response.</returns>
    Message BuildAssistantMessage(string content);

    /// <summary>
    /// Builds a complete conversation from a list of messages.
    /// </summary>
    /// <param name="messages">The messages to include.</param>
    /// <returns>A structured conversation.</returns>
    Conversation BuildConversation(params Message[] messages);

    /// <summary>
    /// Adds context to a prompt (e.g., page state, previous actions).
    /// </summary>
    /// <param name="conversation">The conversation to enhance.</param>
    /// <param name="context">Context data to add.</param>
    /// <returns>The enhanced conversation.</returns>
    Conversation AddContext(Conversation conversation, Dictionary<string, object> context);

    /// <summary>
    /// Formats a prompt template with the given variables.
    /// </summary>
    /// <param name="template">The prompt template.</param>
    /// <param name="variables">Variables to substitute.</param>
    /// <returns>The formatted prompt.</returns>
    string FormatTemplate(string template, Dictionary<string, object> variables);

    #endregion

    #region Advanced Prompt Building

    /// <summary>
    /// Creates a new prompt with the specified system instruction.
    /// </summary>
    /// <param name="systemInstructionKey">The key for the system instruction to use.</param>
    /// <param name="version">Optional version of the system instruction.</param>
    /// <returns>A new prompt instance.</returns>
    Prompt CreatePrompt(string systemInstructionKey = "default", string? version = null);

    /// <summary>
    /// Adds a system instruction to the prompt.
    /// </summary>
    /// <param name="prompt">The prompt to modify.</param>
    /// <param name="instruction">The system instruction text.</param>
    /// <returns>The modified prompt for chaining.</returns>
    Prompt WithSystemInstruction(Prompt prompt, string instruction);

    /// <summary>
    /// Adds context to the prompt.
    /// </summary>
    /// <param name="prompt">The prompt to modify.</param>
    /// <param name="context">The context text or template.</param>
    /// <returns>The modified prompt for chaining.</returns>
    Prompt WithContext(Prompt prompt, string context);

    /// <summary>
    /// Adds user instruction to the prompt.
    /// </summary>
    /// <param name="prompt">The prompt to modify.</param>
    /// <param name="instruction">The user instruction text.</param>
    /// <returns>The modified prompt for chaining.</returns>
    Prompt WithUserInstruction(Prompt prompt, string instruction);

    /// <summary>
    /// Adds tool definitions to the prompt.
    /// </summary>
    /// <param name="prompt">The prompt to modify.</param>
    /// <param name="tools">The list of browser tools.</param>
    /// <returns>The modified prompt for chaining.</returns>
    Prompt WithTools(Prompt prompt, List<BrowserTool> tools);

    /// <summary>
    /// Adds examples to the prompt.
    /// </summary>
    /// <param name="prompt">The prompt to modify.</param>
    /// <param name="examples">The examples text or template.</param>
    /// <returns>The modified prompt for chaining.</returns>
    Prompt WithExamples(Prompt prompt, string examples);

    /// <summary>
    /// Adds output format instructions to the prompt.
    /// </summary>
    /// <param name="prompt">The prompt to modify.</param>
    /// <param name="format">The output format instructions.</param>
    /// <returns>The modified prompt for chaining.</returns>
    Prompt WithOutputFormat(Prompt prompt, string format);

    /// <summary>
    /// Adds a template variable to the prompt.
    /// </summary>
    /// <param name="prompt">The prompt to modify.</param>
    /// <param name="key">The variable key.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>The modified prompt for chaining.</returns>
    Prompt WithVariable(Prompt prompt, string key, string value);

    /// <summary>
    /// Adds multiple template variables to the prompt.
    /// </summary>
    /// <param name="prompt">The prompt to modify.</param>
    /// <param name="variables">The variables dictionary.</param>
    /// <returns>The modified prompt for chaining.</returns>
    Prompt WithVariables(Prompt prompt, Dictionary<string, string> variables);

    /// <summary>
    /// Builds the final prompt text with all components and variables resolved.
    /// </summary>
    /// <param name="prompt">The prompt to build.</param>
    /// <returns>The build result containing the final prompt text.</returns>
    PromptBuildResult Build(Prompt prompt);

    /// <summary>
    /// Builds the final prompt text asynchronously.
    /// </summary>
    /// <param name="prompt">The prompt to build.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result containing the final prompt text.</returns>
    Task<PromptBuildResult> BuildAsync(Prompt prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new system instruction.
    /// </summary>
    /// <param name="instruction">The system instruction to register.</param>
    void RegisterSystemInstruction(SystemInstruction instruction);

    /// <summary>
    /// Gets a system instruction by key and optional version.
    /// </summary>
    /// <param name="key">The instruction key.</param>
    /// <param name="version">Optional specific version.</param>
    /// <returns>The system instruction if found; otherwise, null.</returns>
    SystemInstruction? GetSystemInstruction(string key, string? version = null);

    /// <summary>
    /// Lists all available system instructions.
    /// </summary>
    /// <returns>A list of all registered system instructions.</returns>
    List<SystemInstruction> ListSystemInstructions();

    /// <summary>
    /// Registers a new prompt template.
    /// </summary>
    /// <param name="template">The prompt template to register.</param>
    void RegisterTemplate(PromptTemplate template);

    /// <summary>
    /// Gets a prompt template by name and optional version.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <param name="version">Optional specific version.</param>
    /// <returns>The template if found; otherwise, null.</returns>
    PromptTemplate? GetTemplate(string name, string? version = null);

    /// <summary>
    /// Creates a prompt from a registered template.
    /// </summary>
    /// <param name="templateName">The template name.</param>
    /// <param name="variables">Variables to populate the template.</param>
    /// <param name="version">Optional specific version.</param>
    /// <returns>A new prompt built from the template.</returns>
    Prompt FromTemplate(string templateName, Dictionary<string, string>? variables = null, string? version = null);

    /// <summary>
    /// Validates a prompt for potential injection attacks.
    /// </summary>
    /// <param name="prompt">The prompt to validate.</param>
    /// <returns>A list of detected issues (empty if clean).</returns>
    List<string> ValidateInjection(Prompt prompt);

    /// <summary>
    /// Sanitizes user input to prevent injection attacks.
    /// </summary>
    /// <param name="input">The user input to sanitize.</param>
    /// <returns>The sanitized input.</returns>
    string SanitizeInput(string input);

    /// <summary>
    /// Estimates the token count for a prompt.
    /// </summary>
    /// <param name="text">The text to estimate tokens for.</param>
    /// <returns>The estimated token count.</returns>
    int EstimateTokens(string text);

    /// <summary>
    /// Gets or sets the injection protection options.
    /// </summary>
    InjectionProtectionOptions InjectionOptions { get; set; }

    #endregion
}

/// <summary>
/// Represents a conversation between user and assistant.
/// </summary>
public sealed class Conversation
{
    /// <summary>
    /// Gets or sets the conversation ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the messages in this conversation.
    /// </summary>
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets metadata about the conversation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the conversation started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Adds a message to the conversation.
    /// </summary>
    /// <param name="message">The message to add.</param>
    public void AddMessage(Message message) => Messages.Add(message);
}
