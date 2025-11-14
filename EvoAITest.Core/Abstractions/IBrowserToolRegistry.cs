namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Defines the contract for a browser tool registry.
/// Provides access to browser automation tool definitions for LLMs.
/// </summary>
public interface IBrowserToolRegistry
{
    /// <summary>
    /// Gets all available browser automation tools.
    /// </summary>
    /// <returns>A list of all registered browser tools.</returns>
    List<Models.BrowserToolDefinition> GetAllTools();

    /// <summary>
    /// Gets a specific browser tool by name.
    /// </summary>
    /// <param name="name">The name of the tool (case-insensitive).</param>
    /// <returns>The browser tool definition.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the tool does not exist.</exception>
    Models.BrowserToolDefinition GetTool(string name);

    /// <summary>
    /// Checks if a tool with the specified name exists in the registry.
    /// </summary>
    /// <param name="name">The name of the tool to check (case-insensitive).</param>
    /// <returns>True if the tool exists; otherwise, false.</returns>
    bool ToolExists(string name);

    /// <summary>
    /// Gets all tools as a JSON string suitable for sending to LLM APIs.
    /// </summary>
    /// <returns>JSON string representation of all tools.</returns>
    string GetToolsAsJson();

    /// <summary>
    /// Gets the names of all available tools.
    /// </summary>
    /// <returns>An array of tool names.</returns>
    string[] GetToolNames();

    /// <summary>
    /// Gets the count of registered tools.
    /// </summary>
    int ToolCount { get; }
}
