using EvoAITest.LLM.Models;

namespace EvoAITest.LLM.Abstractions;

/// <summary>
/// Defines a contract for building and managing LLM prompts.
/// </summary>
public interface IPromptBuilder
{
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
