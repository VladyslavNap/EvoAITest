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
            ),

            ["visual_check"] = new BrowserToolDefinition(
                Name: "visual_check",
                Description: "Capture a screenshot and compare it against a baseline image for visual regression testing. Use at key points to verify UI consistency.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["checkpoint_name"] = new ParameterDef("string", true, "Unique name for this visual checkpoint (e.g., 'HomePage_AfterLoad', 'LoginForm_BeforeSubmit')", null),
                    ["checkpoint_type"] = new ParameterDef("string", false, "Type of screenshot: 'fullpage', 'viewport', 'element', or 'region'", "fullpage"),
                    ["selector"] = new ParameterDef("string", false, "CSS selector for element-based screenshots (required if checkpoint_type is 'element')", null),
                    ["region"] = new ParameterDef("string", false, "Region coordinates as JSON '{\"x\":0,\"y\":0,\"width\":100,\"height\":100}' for partial screenshots", null),
                    ["tolerance"] = new ParameterDef("string", false, "Comparison tolerance (0.0-1.0, default 0.01 = 1% difference allowed)", "0.01"),
                    ["ignore_selectors"] = new ParameterDef("array", false, "Array of CSS selectors for elements to ignore during comparison (e.g., timestamps, ads)", null),
                    ["create_baseline_if_missing"] = new ParameterDef("boolean", false, "Auto-create baseline if it doesn't exist (useful for first run)", false)
                }
            ),

            // ========== Mobile Device Emulation Tools ==========

            ["set_device_emulation"] = new BrowserToolDefinition(
                Name: "set_device_emulation",
                Description: "Emulate a specific mobile device including viewport, user agent, touch support, and device metrics. Use this before navigating to test mobile responsive designs. Supports predefined devices (iPhone14Pro, GalaxyS23, iPadPro11, etc.) or custom configurations.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["device_name"] = new ParameterDef("string", false, "Name of predefined device to emulate (e.g., 'iPhone14Pro', 'GalaxyS23', 'iPadPro11', 'Pixel7'). See DevicePresets for full list.", null),
                    ["viewport_width"] = new ParameterDef("int", false, "Custom viewport width in pixels (required if device_name not specified)", null),
                    ["viewport_height"] = new ParameterDef("int", false, "Custom viewport height in pixels (required if device_name not specified)", null),
                    ["user_agent"] = new ParameterDef("string", false, "Custom user agent string (uses device default if not specified)", null),
                    ["device_scale_factor"] = new ParameterDef("string", false, "Device pixel ratio (e.g., '2.0' for Retina, '3.0' for high-DPI)", "1.0"),
                    ["has_touch"] = new ParameterDef("boolean", false, "Enable touch event support", true),
                    ["is_mobile"] = new ParameterDef("boolean", false, "Indicate mobile mode (affects meta viewport and touch)", true)
                }
            ),

            ["set_geolocation"] = new BrowserToolDefinition(
                Name: "set_geolocation",
                Description: "Set the browser's geolocation to specific GPS coordinates. Useful for testing location-based features. Requires 'geolocation' permission to be granted. Supports preset locations (SanFrancisco, NewYork, London, Tokyo, Sydney, Paris) or custom coordinates.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["preset"] = new ParameterDef("string", false, "Use preset location: 'SanFrancisco', 'NewYork', 'London', 'Tokyo', 'Sydney', or 'Paris'", null),
                    ["latitude"] = new ParameterDef("string", false, "Latitude in decimal degrees (-90 to 90). Required if preset not specified.", null),
                    ["longitude"] = new ParameterDef("string", false, "Longitude in decimal degrees (-180 to 180). Required if preset not specified.", null),
                    ["accuracy"] = new ParameterDef("string", false, "Optional accuracy in meters (e.g., '10.0' for 10 meter accuracy)", null)
                }
            ),

            ["set_timezone"] = new BrowserToolDefinition(
                Name: "set_timezone",
                Description: "Set the browser's timezone for testing time-sensitive features. Note: Due to Playwright limitations, timezone should ideally be set before browser initialization. This tool logs a warning but can be useful for documentation purposes.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["timezone_id"] = new ParameterDef("string", true, "IANA timezone identifier (e.g., 'America/New_York', 'Europe/London', 'Asia/Tokyo')", null)
                }
            ),

            ["set_locale"] = new BrowserToolDefinition(
                Name: "set_locale",
                Description: "Set the browser's language and regional preferences. Affects the Accept-Language header and how web content is displayed. Useful for testing internationalization (i18n) and localization (l10n).",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["locale"] = new ParameterDef("string", true, "BCP 47 language tag (e.g., 'en-US', 'fr-FR', 'ja-JP', 'es-ES', 'de-DE')", null)
                }
            ),

            ["grant_permissions"] = new BrowserToolDefinition(
                Name: "grant_permissions",
                Description: "Grant browser permissions that would normally require user interaction. Essential for testing features like geolocation, notifications, camera, microphone access, etc. Permissions persist for the browser session.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["permissions"] = new ParameterDef("array", true, "Array of permission names to grant. Common values: 'geolocation', 'notifications', 'camera', 'microphone', 'clipboard-read', 'clipboard-write'", null)
                }
            ),

            ["clear_permissions"] = new BrowserToolDefinition(
                Name: "clear_permissions",
                Description: "Revoke all previously granted browser permissions. Use this to test permission denial scenarios or reset permission state between tests.",
                Parameters: new Dictionary<string, ParameterDef>()
            ),

            // ========== Network Interception Tools ==========

            ["mock_response"] = new BrowserToolDefinition(
                Name: "mock_response",
                Description: "Mock HTTP responses for requests matching a URL pattern. Intercepts network requests and returns custom responses without making actual network calls. Useful for testing error states, offline scenarios, or controlling API responses. Supports glob patterns (e.g., '**/api/**', '**/*.json') and delays to simulate network latency.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["url_pattern"] = new ParameterDef("string", true, "URL pattern to match (glob syntax: '**/api/users', '**/*.jpg'). Use '**' for any path segment.", null),
                    ["status"] = new ParameterDef("int", false, "HTTP status code to return (e.g., 200, 404, 500)", 200),
                    ["body"] = new ParameterDef("string", false, "Response body content (JSON, HTML, text, etc.)", null),
                    ["content_type"] = new ParameterDef("string", false, "Content-Type header value (e.g., 'application/json', 'text/html')", "application/json"),
                    ["delay_ms"] = new ParameterDef("int", false, "Delay in milliseconds before responding (simulates network latency)", 0),
                    ["headers"] = new ParameterDef("array", false, "Additional response headers as array of 'name:value' strings", null)
                }
            ),

            ["block_request"] = new BrowserToolDefinition(
                Name: "block_request",
                Description: "Block network requests matching a URL pattern. Prevents requests from being sent to the server. Useful for blocking ads, trackers, analytics, images, or specific API endpoints to test offline behavior or improve test performance. Blocked requests fail immediately without network activity.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["url_pattern"] = new ParameterDef("string", true, "URL pattern to block (glob syntax: '**/*.{jpg,png,gif}' for images, '**/analytics/**' for analytics, '**/ads/**' for ads)", null)
                }
            ),

            ["intercept_request"] = new BrowserToolDefinition(
                Name: "intercept_request",
                Description: "Set up custom request interception with a handler. Advanced tool for modifying requests on-the-fly, adding headers, changing payloads, or conditionally mocking based on request details. This tool sets up the interception pattern; actual handling is done by the automation framework.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["url_pattern"] = new ParameterDef("string", true, "URL pattern to intercept (glob syntax)", null),
                    ["action"] = new ParameterDef("string", true, "Action to take: 'continue' (pass through), 'abort' (block), or 'fulfill' (mock)", "continue")
                }
            ),

            ["get_network_logs"] = new BrowserToolDefinition(
                Name: "get_network_logs",
                Description: "Retrieve all captured network activity logs. Returns details about HTTP requests made by the page including URLs, methods, status codes, timing, and whether requests were blocked or mocked. Useful for verifying API calls, debugging network issues, or validating that specific requests occurred. Network logging must be enabled first.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["enable_logging"] = new ParameterDef("boolean", false, "Enable network logging if not already enabled", true)
                }
            ),

            ["clear_interceptions"] = new BrowserToolDefinition(
                Name: "clear_interceptions",
                Description: "Clear all active network interceptions (mocks, blocks, and custom handlers). Removes all route handlers and allows requests to proceed normally. Use this to reset network state between test scenarios or when switching from mocked to real API testing.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["clear_logs"] = new ParameterDef("boolean", false, "Also clear the network activity logs", false)
                }
            ),

            // Smart Waiting Tools
            ["smart_wait"] = new BrowserToolDefinition(
                Name: "smart_wait",
                Description: "Intelligent adaptive waiting for multiple page stability conditions. Waits for DOM stability, animations completion, network idle, and loading indicators to disappear. Use this for complex pages with dynamic content, AJAX calls, or animations. More reliable than fixed timeouts as it adapts to actual page behavior.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["conditions"] = new ParameterDef("array", false, "Array of conditions to wait for: 'dom_stable', 'animations_complete', 'network_idle', 'loaders_hidden'. Default: all conditions", null),
                    ["require_all"] = new ParameterDef("boolean", false, "If true, all conditions must be met (AND). If false, any condition is sufficient (OR)", true),
                    ["max_wait_ms"] = new ParameterDef("int", false, "Maximum time to wait in milliseconds", 10000),
                    ["selector"] = new ParameterDef("string", false, "Optional selector to check conditions for specific element", null)
                }
            ),

            ["wait_for_stable"] = new BrowserToolDefinition(
                Name: "wait_for_stable",
                Description: "Wait for the page to reach a completely stable state (DOM stable + no animations + no network activity + no loaders). This is the most comprehensive wait ensuring the page is fully loaded and ready for interaction. Use after navigation, form submissions, or any action that triggers page updates. Recommended for flaky tests or complex SPAs.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["max_wait_ms"] = new ParameterDef("int", false, "Maximum time to wait in milliseconds", 10000),
                    ["stability_period_ms"] = new ParameterDef("int", false, "How long the page must remain stable before considering it ready", 500)
                }
            ),

            ["wait_for_animations"] = new BrowserToolDefinition(
                Name: "wait_for_animations",
                Description: "Wait for CSS animations and transitions to complete on the page or a specific element. Detects running animations using the Web Animations API. Use this after triggering animations (hover, click), before taking screenshots, or when waiting for modal/drawer animations. Essential for visual regression tests.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["selector"] = new ParameterDef("string", false, "Optional CSS selector to check animations for specific element. If not provided, checks all page animations", null),
                    ["max_wait_ms"] = new ParameterDef("int", false, "Maximum time to wait for animations to complete", 5000)
                }
            ),

            ["wait_for_network_idle"] = new BrowserToolDefinition(
                Name: "wait_for_network_idle",
                Description: "Wait for network activity to become idle (no active HTTP requests). Useful after actions that trigger API calls, when loading data, or before assertions that depend on server responses. More reliable than fixed waits as it adapts to actual network speed and API response times.",
                Parameters: new Dictionary<string, ParameterDef>
                {
                    ["max_active_requests"] = new ParameterDef("int", false, "Maximum number of active requests to consider as 'idle' (0 = no requests, 2 = up to 2 concurrent requests allowed)", 0),
                    ["idle_duration_ms"] = new ParameterDef("int", false, "How long network must be idle before considering it stable", 500),
                    ["max_wait_ms"] = new ParameterDef("int", false, "Maximum time to wait for network idle", 10000)
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
