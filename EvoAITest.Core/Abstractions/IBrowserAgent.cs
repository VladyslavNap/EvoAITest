using EvoAITest.Core.Models;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Defines the core contract for high-level browser automation operations.
/// This interface provides a simplified, agent-friendly API for browser automation tasks,
/// abstracting away low-level browser driver details.
/// </summary>
/// <remarks>
/// <para>
/// This interface is designed for use in .NET Aspire containerized environments.
/// All methods support graceful shutdown via <see cref="CancellationToken"/> to enable
/// proper cleanup when the Aspire orchestrator signals termination.
/// </para>
/// <para>
/// Implementations should be registered as scoped services in the DI container
/// and must properly handle resource cleanup in the <see cref="IAsyncDisposable.DisposeAsync"/> method.
/// </para>
/// <para>
/// For Aspire container deployments, consider setting appropriate timeouts in
/// the orchestrator configuration to allow for browser initialization and cleanup.
/// </para>
/// </remarks>
public interface IBrowserAgent : IAsyncDisposable
{
    /// <summary>
    /// Initializes the browser agent and prepares it for automation operations.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the initialization.
    /// This is particularly important in Aspire environments for graceful shutdown.
    /// </param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    /// <remarks>
    /// <para>
    /// This method should be called once before any other operations.
    /// It typically launches the browser process, creates contexts, and prepares pages.
    /// </para>
    /// <para>
    /// In containerized Aspire deployments, this method may take several seconds
    /// as it downloads browser binaries (if needed) and initializes the browser process.
    /// Ensure your health check grace period accounts for this initialization time.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the agent is already initialized or if initialization fails.
    /// </exception>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the browser page, including URL, title, and interactive elements.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a <see cref="PageState"/> object with the current page information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method captures a snapshot of the current page state, which is essential
    /// for AI agents to understand the page structure and determine next actions.
    /// </para>
    /// <para>
    /// The returned state includes all interactive elements (buttons, inputs, links)
    /// visible on the page, making it suitable for feeding to LLM-based planners.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized or the page is closed.
    /// </exception>
    Task<PageState> GetPageStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates the browser to the specified URL.
    /// </summary>
    /// <param name="url">The URL to navigate to. Must be a valid HTTP(S) URL.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the navigation operation.
    /// </param>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    /// <remarks>
    /// <para>
    /// This method waits for the page to reach a stable state before returning
    /// (typically waiting for 'load' or 'networkidle' events).
    /// </para>
    /// <para>
    /// In Aspire container environments, network access may be subject to container
    /// networking policies. Ensure your container has appropriate network permissions.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="url"/> is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown if the navigation times out (typically after 30 seconds).
    /// </exception>
    Task NavigateAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clicks the element matching the specified CSS selector.
    /// </summary>
    /// <param name="selector">
    /// A CSS selector string that identifies the element to click.
    /// </param>
    /// <param name="maxRetries">
    /// The maximum number of retry attempts if the element is not immediately clickable.
    /// Default is 3 retries.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the click operation.
    /// </param>
    /// <returns>A task that represents the asynchronous click operation.</returns>
    /// <remarks>
    /// <para>
    /// This method automatically waits for the element to be visible, enabled,
    /// and stable before attempting to click. It includes built-in retry logic
    /// to handle dynamic page content.
    /// </para>
    /// <para>
    /// If the element is not immediately clickable, the method will retry up to
    /// <paramref name="maxRetries"/> times with exponential backoff.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="selector"/> is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown if the element cannot be found or clicked within the timeout period,
    /// even after all retry attempts.
    /// </exception>
    Task ClickAsync(string selector, int maxRetries = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Types the specified text into the element matching the CSS selector.
    /// </summary>
    /// <param name="selector">
    /// A CSS selector string that identifies the target input element.
    /// </param>
    /// <param name="text">
    /// The text to type into the element. Each character is typed with a small delay
    /// to simulate human typing.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the typing operation.
    /// </param>
    /// <returns>A task that represents the asynchronous typing operation.</returns>
    /// <remarks>
    /// <para>
    /// This method types text character-by-character with realistic delays,
    /// useful for triggering input events that some web applications rely on.
    /// For faster input without character-by-character delays, consider using
    /// the Fill method on the underlying element.
    /// </para>
    /// <para>
    /// The element must be an input, textarea, or other text-editable element.
    /// The method waits for the element to be visible and enabled before typing.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="selector"/> or <paramref name="text"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized or if the target element
    /// is not a text input element.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown if the element cannot be found within the timeout period.
    /// </exception>
    Task TypeAsync(string selector, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the visible text content of the element matching the specified CSS selector.
    /// </summary>
    /// <param name="selector">
    /// A CSS selector string that identifies the element to extract text from.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the visible text content of the element,
    /// or an empty string if the element has no text.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns only the visible text content, excluding hidden elements,
    /// script tags, style tags, and other non-visible content.
    /// </para>
    /// <para>
    /// Useful for extracting data from the page for verification or analysis by AI agents.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="selector"/> is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown if the element cannot be found within the timeout period.
    /// </exception>
    Task<string> GetTextAsync(string selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a screenshot of the current page and returns it as a base64-encoded string.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the screenshot operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a base64-encoded PNG image of the page.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Screenshots are useful for debugging, logging, and providing visual context
    /// to AI agents analyzing page state. The image captures the current viewport.
    /// </para>
    /// <para>
    /// In Aspire container environments, consider the memory implications of
    /// storing base64-encoded images, especially for high-volume automation scenarios.
    /// Consider implementing a separate blob storage solution for production use.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized.
    /// </exception>
    Task<string> TakeScreenshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a text-based representation of the page's accessibility tree,
    /// useful for AI analysis and understanding page structure.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a formatted string representing the accessibility tree,
    /// showing the hierarchical structure of semantic elements on the page.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The accessibility tree provides a semantic view of the page structure,
    /// including roles, labels, and hierarchical relationships. This is particularly
    /// useful for LLM-based agents to understand page layout without visual processing.
    /// </para>
    /// <para>
    /// The returned string is typically formatted as an indented tree structure,
    /// making it easy to include in AI prompts for context.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized.
    /// </exception>
    Task<string> GetAccessibilityTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for an element matching the specified CSS selector to appear on the page.
    /// </summary>
    /// <param name="selector">
    /// A CSS selector string that identifies the element to wait for.
    /// </param>
    /// <param name="timeoutMs">
    /// The maximum time to wait in milliseconds. Default is 30000 (30 seconds).
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the wait operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous wait operation.
    /// The task completes when the element appears or the timeout is reached.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is essential for handling dynamic content that loads asynchronously.
    /// It polls the page for the element at regular intervals until it appears
    /// or the timeout expires.
    /// </para>
    /// <para>
    /// The element is considered "appeared" when it exists in the DOM and is visible.
    /// Hidden or display:none elements will not satisfy the wait condition.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="selector"/> is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown if the element does not appear within the specified timeout period.
    /// </exception>
    Task WaitForElementAsync(string selector, int timeoutMs = 30000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete HTML source of the current page.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the full HTML source code of the page,
    /// including any dynamic content that has been rendered.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns the fully rendered HTML, including any content generated
    /// by JavaScript. It captures the current state of the DOM, not the original
    /// HTML sent by the server.
    /// </para>
    /// <para>
    /// For large pages, the returned HTML can be substantial. In Aspire container
    /// environments with memory constraints, consider processing the HTML in chunks
    /// or extracting only the needed portions.
    /// </para>
    /// <para>
    /// This is useful for detailed page analysis, debugging, or when you need to
    /// parse the page structure programmatically.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized.
    /// </exception>
    Task<string> GetPageHtmlAsync(CancellationToken cancellationToken = default);
}
