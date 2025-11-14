using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;

namespace EvoAITest.Core.Services;

/// <summary>
/// Default implementation of <see cref="IBrowserToolRegistry"/> that wraps the static <see cref="BrowserToolRegistry"/>.
/// </summary>
internal sealed class DefaultBrowserToolRegistry : IBrowserToolRegistry
{
    /// <inheritdoc/>
    public List<BrowserToolDefinition> GetAllTools() => BrowserToolRegistry.GetAllTools();

    /// <inheritdoc/>
    public BrowserToolDefinition GetTool(string name) => BrowserToolRegistry.GetTool(name);

    /// <inheritdoc/>
    public bool ToolExists(string name) => BrowserToolRegistry.ToolExists(name);

    /// <inheritdoc/>
    public string GetToolsAsJson() => BrowserToolRegistry.GetToolsAsJson();

    /// <inheritdoc/>
    public string[] GetToolNames() => BrowserToolRegistry.GetToolNames();

    /// <inheritdoc/>
    public int ToolCount => BrowserToolRegistry.ToolCount;
}
