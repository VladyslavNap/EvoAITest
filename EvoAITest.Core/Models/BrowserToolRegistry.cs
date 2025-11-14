using System.Text.Json;

namespace EvoAITest.Core.Models;

/// <summary>
/// Defines a parameter for a browser automation tool (immutable).
/// </summary>
/// <param name="Type">The parameter type: "string", "int", "boolean", or "array".</param>
/// <param name="Required">Indicates whether this parameter is required.</param>
/// <param name="Description">Human-readable description of what this parameter does.</param>
/// <param name="DefaultValue">Optional default value if parameter is not provided (null if no default).</param>
public sealed record ParameterDef(
    string Type,
    bool Required,
    string Description,
    object? DefaultValue
);

/// <summary>
/// Represents a browser automation tool definition that can be called by an LLM (immutable).
/// </summary>
/// <param name="Name">The unique identifier for this tool (e.g., "click", "type", "navigate").</param>
/// <param name="Description">Human-readable description of what this tool does and when to use it.</param>
/// <param name="Parameters">Dictionary of parameter names to their definitions.</param>
public sealed record BrowserToolDefinition(
    string Name,
    string Description,
    Dictionary<string, ParameterDef> Parameters
);

/// <summary>
/// Static registry containing all available browser automation tools.
/// This provides a centralized definition of tools that LLMs can use for browser automation.
/// </summary>
public static class BrowserToolRegistry
{
    private static readonly Dictionary<string, BrowserToolDefinition> _tools;

    static BrowserToolRegistry()
    {
        _tools = new Dictionary<string, BrowserToolDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["navigate"] = new BrowserToolDefinition(
                Name: "navigate",
                Description: "Navigate the browser to a specific URL. Use this to load a new page or change the current page.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["url"] = new ParameterDef("string", true, "The URL to navigate to (must include protocol like https://)", null),
                    ["wait_until"] = new ParameterDef("string", false, "Wait until page reaches this state: 'load', 'domcontentloaded', or 'networkidle'", "load")
                }
            ),

            ["click"] = new BrowserToolDefinition(
                Name: "click",
                Description: "Click on an element identified by a CSS selector. Use for buttons, links, checkboxes, radio buttons, etc.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the element to click", null),
                    ["button"] = new ParameterDef("string", false, "Mouse button to use: 'left', 'right', or 'middle'", "left"),
                    ["click_count"] = new ParameterDef("int", false, "Number of times to click (for double-click use 2)", 1),
                    ["force"] = new ParameterDef("boolean", false, "Force the click even if element is not visible or enabled", false)
                }
            ),

            ["type"] = new BrowserToolDefinition(
                Name: "type",
                Description: "Type text into an input field, textarea, or contenteditable element. Text is typed character by character with realistic delays.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the input element", null),
                    ["text"] = new ParameterDef("string", true, "The text to type into the element", null),
                    ["delay_ms"] = new ParameterDef("int", false, "Delay in milliseconds between keystrokes (simulates human typing)", 50),
                    ["clear_first"] = new ParameterDef("boolean", false, "Clear existing text before typing", false)
                }
            ),

            ["clear_input"] = new BrowserToolDefinition(
                Name: "clear_input",
                Description: "Clear all text from an input field or textarea. Use before typing new text if you want to replace existing content.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the input element to clear", null)
                }
            ),

            ["extract_text"] = new BrowserToolDefinition(
                Name: "extract_text",
                Description: "Extract visible text content from an element or multiple elements. Useful for reading page content, error messages, labels, etc.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the element(s) to extract text from", null),
                    ["all_matches"] = new ParameterDef("boolean", false, "Extract text from all matching elements (true) or just the first (false)", false),
                    ["include_hidden"] = new ParameterDef("boolean", false, "Include text from hidden elements", false)
                }
            ),

            ["extract_table"] = new BrowserToolDefinition(
                Name: "extract_table",
                Description: "Extract structured data from an HTML table. Returns data as rows and columns with headers.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the table element", null),
                    ["include_headers"] = new ParameterDef("boolean", false, "Include table headers in the extracted data", true),
                    ["format"] = new ParameterDef("string", false, "Output format: 'json' or 'csv'", "json")
                }
            ),

            ["get_page_state"] = new BrowserToolDefinition(
                Name: "get_page_state",
                Description: "Get comprehensive information about the current page including URL, title, interactive elements, and page structure. Use to understand what's on the page.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["include_hidden"] = new ParameterDef("boolean", false, "Include hidden elements in the state", false),
                    ["include_screenshots"] = new ParameterDef("boolean", false, "Include base64-encoded screenshot in the state", false),
                    ["include_console"] = new ParameterDef("boolean", false, "Include console messages and errors", true)
                }
            ),

            ["take_screenshot"] = new BrowserToolDefinition(
                Name: "take_screenshot",
                Description: "Capture a screenshot of the current page or a specific element. Returns base64-encoded PNG image.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", false, "CSS selector to screenshot a specific element (null for full page)", null),
                    ["full_page"] = new ParameterDef("boolean", false, "Capture full scrollable page (true) or just viewport (false)", false),
                    ["quality"] = new ParameterDef("int", false, "JPEG quality (0-100) if using JPEG format, ignored for PNG", 80)
                }
            ),

            ["wait_for_element"] = new BrowserToolDefinition(
                Name: "wait_for_element",
                Description: "Wait for an element to appear on the page and become visible. Use when waiting for dynamic content to load.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the element to wait for", null),
                    ["state"] = new ParameterDef("string", false, "Wait for element to reach this state: 'visible', 'attached', 'hidden', or 'detached'", "visible"),
                    ["timeout_ms"] = new ParameterDef("int", false, "Maximum time to wait in milliseconds", 30000)
                }
            ),

            ["wait_for_url_change"] = new BrowserToolDefinition(
                Name: "wait_for_url_change",
                Description: "Wait for the page URL to change to a specific URL or match a pattern. Useful after submitting forms or clicking navigation links.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["expected_url"] = new ParameterDef("string", false, "Exact URL to wait for (null to wait for any change)", null),
                    ["url_pattern"] = new ParameterDef("string", false, "Regex pattern to match against the URL", null),
                    ["timeout_ms"] = new ParameterDef("int", false, "Maximum time to wait in milliseconds", 30000)
                }
            ),

            ["select_option"] = new BrowserToolDefinition(
                Name: "select_option",
                Description: "Select an option from a dropdown (select element) by value, label, or index.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the select element", null),
                    ["value"] = new ParameterDef("string", false, "Option value attribute to select", null),
                    ["label"] = new ParameterDef("string", false, "Option visible text to select", null),
                    ["index"] = new ParameterDef("int", false, "Zero-based index of option to select", null)
                }
            ),

            ["submit_form"] = new BrowserToolDefinition(
                Name: "submit_form",
                Description: "Submit a form by clicking its submit button or programmatically submitting it. Use after filling form fields.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the form element", null),
                    ["wait_for_navigation"] = new ParameterDef("boolean", false, "Wait for page navigation after form submission", true),
                    ["timeout_ms"] = new ParameterDef("int", false, "Maximum time to wait for navigation in milliseconds", 30000)
                }
            ),

            ["verify_element_exists"] = new BrowserToolDefinition(
                Name: "verify_element_exists",
                Description: "Check if an element exists on the page and optionally verify its state or content. Returns boolean result.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", true, "CSS selector to identify the element to verify", null),
                    ["expected_text"] = new ParameterDef("string", false, "Expected text content (null to skip text verification)", null),
                    ["should_be_visible"] = new ParameterDef("boolean", false, "Element must be visible (true) or just exist in DOM (false)", true),
                    ["timeout_ms"] = new ParameterDef("int", false, "Maximum time to wait for element in milliseconds", 5000)
                }
            )
        };
    }

    /// <summary>
    /// Gets all available browser automation tools.
    /// </summary>
    /// <returns>A list of all registered browser tools.</returns>
    public static List<BrowserToolDefinition> GetAllTools()
    {
        return _tools.Values.ToList();
    }

    /// <summary>
    /// Gets a specific browser tool by name.
    /// </summary>
    /// <param name="name">The name of the tool (case-insensitive).</param>
    /// <returns>The browser tool definition.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the tool does not exist.</exception>
    public static BrowserToolDefinition GetTool(string name)
    {
        if (!_tools.TryGetValue(name, out var tool))
        {
            throw new KeyNotFoundException($"Browser tool '{name}' not found. Available tools: {string.Join(", ", _tools.Keys)}");
        }
        return tool;
    }

    /// <summary>
    /// Checks if a tool with the specified name exists in the registry.
    /// </summary>
    /// <param name="name">The name of the tool to check (case-insensitive).</param>
    /// <returns>True if the tool exists; otherwise, false.</returns>
    public static bool ToolExists(string name)
    {
        return _tools.ContainsKey(name);
    }

    /// <summary>
    /// Gets all tools as a JSON string suitable for sending to LLM APIs.
    /// The JSON format follows the OpenAI function calling specification.
    /// </summary>
    /// <returns>JSON string representation of all tools.</returns>
    public static string GetToolsAsJson()
    {
        var toolsForLLM = _tools.Values.Select(tool => new
        {
            type = "function",
            function = new
            {
                name = tool.Name,
                description = tool.Description,
                parameters = new
                {
                    type = "object",
                    properties = tool.Parameters.ToDictionary(
                        p => p.Key,
                        p => new
                        {
                            type = p.Value.Type,
                            description = p.Value.Description,
                            @default = p.Value.DefaultValue
                        }
                    ),
                    required = tool.Parameters
                        .Where(p => p.Value.Required)
                        .Select(p => p.Key)
                        .ToArray()
                }
            }
        });

        return JsonSerializer.Serialize(toolsForLLM, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Gets the names of all available tools.
    /// </summary>
    /// <returns>An array of tool names.</returns>
    public static string[] GetToolNames()
    {
        return _tools.Keys.ToArray();
    }

    /// <summary>
    /// Gets the count of registered tools.
    /// </summary>
    public static int ToolCount => _tools.Count;
}
