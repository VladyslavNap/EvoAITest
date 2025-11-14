using EvoAITest.Core.Models;
using EvoAITest.Core.Options;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Defines the core contract for browser automation drivers.
/// Implementations wrap browser automation libraries (Playwright, Selenium, etc.).
/// </summary>
public interface IBrowserDriver : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the browser driver implementation.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the browser is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Initializes and launches the browser with the specified options.
    /// </summary>
    /// <param name="options">Browser launch options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the browser is launched.</returns>
    Task LaunchAsync(BrowserOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new browser context (isolated session).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new browser context.</returns>
    Task<IBrowserContext> CreateContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the browser and releases resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the browser is closed.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the browser version information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Browser version string.</returns>
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an isolated browser session with independent cookies, cache, and storage.
/// </summary>
public interface IBrowserContext : IAsyncDisposable
{
    /// <summary>
    /// Gets the pages (tabs) in this context.
    /// </summary>
    IReadOnlyList<IPage> Pages { get; }

    /// <summary>
    /// Creates a new page (tab) in this context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new page instance.</returns>
    Task<IPage> NewPageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the context and all its pages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the context is closed.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets cookies for this context.
    /// </summary>
    /// <param name="cookies">Cookies to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when cookies are set.</returns>
    Task SetCookiesAsync(IEnumerable<Cookie> cookies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cookies in this context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All cookies.</returns>
    Task<IEnumerable<Cookie>> GetCookiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cookies in this context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when cookies are cleared.</returns>
    Task ClearCookiesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a browser page (tab).
/// </summary>
public interface IPage : IAsyncDisposable
{
    /// <summary>
    /// Gets the current URL of the page.
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Gets the page title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets a value indicating whether the page is closed.
    /// </summary>
    bool IsClosed { get; }

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    /// <param name="options">Navigation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when navigation is complete.</returns>
    Task<ExecutionResult> NavigateAsync(string url, NavigationOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locates an element on the page.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The located element.</returns>
    Task<IElement> LocateAsync(ElementLocator locator, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locates all elements matching the locator.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All matching elements.</returns>
    Task<IReadOnlyList<IElement>> LocateAllAsync(ElementLocator locator, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for the page to reach the specified load state.
    /// </summary>
    /// <param name="state">The desired load state.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the load state is reached.</returns>
    Task WaitForLoadStateAsync(LoadState state, int timeoutMs = 30000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a screenshot of the page.
    /// </summary>
    /// <param name="fullPage">Whether to capture the full page or just the viewport.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Screenshot data as base64 string.</returns>
    Task<string> ScreenshotAsync(bool fullPage = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes JavaScript code in the page context.
    /// </summary>
    /// <param name="script">The JavaScript code to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the script execution.</returns>
    Task<object?> EvaluateAsync(string script, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page state.</returns>
    Task<PageState> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the page is closed.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a DOM element.
/// </summary>
public interface IElement
{
    /// <summary>
    /// Gets a value indicating whether the element is visible.
    /// </summary>
    Task<bool> IsVisibleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the element is enabled.
    /// </summary>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clicks the element.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result.</returns>
    Task<ExecutionResult> ClickAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Types text into the element.
    /// </summary>
    /// <param name="text">Text to type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result.</returns>
    Task<ExecutionResult> TypeAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fills the element with text (clears first).
    /// </summary>
    /// <param name="text">Text to fill.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result.</returns>
    Task<ExecutionResult> FillAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the text content of the element.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The text content.</returns>
    Task<string?> GetTextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an attribute value of the element.
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The attribute value.</returns>
    Task<string?> GetAttributeAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the element.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Element information.</returns>
    Task<ElementInfo> GetInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a browser cookie.
/// </summary>
public sealed class Cookie
{
    /// <summary>
    /// Gets or sets the cookie name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cookie value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the domain.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cookie is HTTP-only.
    /// </summary>
    public bool HttpOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cookie is secure.
    /// </summary>
    public bool Secure { get; set; }

    /// <summary>
    /// Gets or sets the SameSite attribute.
    /// </summary>
    public string? SameSite { get; set; }
}
