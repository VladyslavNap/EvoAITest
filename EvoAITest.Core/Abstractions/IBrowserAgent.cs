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

    // ========== Visual Regression Screenshot Methods ==========

    /// <summary>
    /// Captures a screenshot of the full page and returns it as a byte array.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains PNG image bytes.</returns>
    /// <remarks>
    /// This method captures the entire scrollable page, not just the current viewport.
    /// Useful for visual regression testing where you need the raw image bytes.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    Task<byte[]> TakeFullPageScreenshotBytesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a screenshot of a specific element and returns it as a byte array.
    /// </summary>
    /// <param name="selector">A CSS selector string that identifies the element to capture.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains PNG image bytes.</returns>
    /// <remarks>
    /// This method waits for the element to be visible before capturing.
    /// Useful for visual regression testing of specific components.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="selector"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    /// <exception cref="TimeoutException">Thrown if the element cannot be found within the timeout period.</exception>
    Task<byte[]> TakeElementScreenshotAsync(string selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a screenshot of a specific rectangular region and returns it as a byte array.
    /// </summary>
    /// <param name="region">The rectangular region to capture (x, y, width, height in pixels).</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains PNG image bytes.</returns>
    /// <remarks>
    /// Useful for visual regression testing of specific areas that don't correspond to single elements.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="region"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    Task<byte[]> TakeRegionScreenshotAsync(ScreenshotRegion region, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a screenshot of the current viewport only and returns it as a byte array.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains PNG image bytes.</returns>
    /// <remarks>
    /// This method captures only the currently visible area, not the full scrollable page.
    /// Faster than full page screenshots and useful for above-the-fold content testing.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    Task<byte[]> TakeViewportScreenshotAsync(CancellationToken cancellationToken = default);

    // ========== Mobile Device Emulation Methods ==========

    /// <summary>
    /// Configures the browser to emulate a specific mobile device.
    /// </summary>
    /// <param name="device">
    /// The device profile to emulate, including viewport, user agent, device metrics, and capabilities.
    /// Use <see cref="DevicePresets"/> for predefined device profiles.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method applies comprehensive device emulation including:
    /// - Viewport dimensions and device pixel ratio
    /// - User agent string
    /// - Touch event support
    /// - Mobile/desktop mode
    /// - Geolocation (if specified in the device profile)
    /// - Timezone (if specified in the device profile)
    /// - Locale (if specified in the device profile)
    /// - Permissions (if specified in the device profile)
    /// </para>
    /// <para>
    /// This method should be called before navigating to a URL to ensure proper device emulation.
    /// Some settings (like viewport) can be changed after navigation, but for best results,
    /// set device emulation first.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var iPhone = DevicePresets.iPhone14Pro;
    /// await browserAgent.SetDeviceEmulationAsync(iPhone);
    /// await browserAgent.NavigateAsync("https://example.com");
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="device"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the browser agent is not initialized or if device emulation cannot be applied.
    /// </exception>
    Task SetDeviceEmulationAsync(DeviceProfile device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the geolocation coordinates for the browser context.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees (-90 to 90).</param>
    /// <param name="longitude">Longitude in decimal degrees (-180 to 180).</param>
    /// <param name="accuracy">Optional accuracy in meters. If null, uses high accuracy (default: 0).</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method sets the geolocation that will be returned by the browser's Geolocation API.
    /// After calling this method, JavaScript code using navigator.geolocation will receive these coordinates.
    /// </para>
    /// <para>
    /// Geolocation must be granted via <see cref="GrantPermissionsAsync"/> before it can be used by web pages.
    /// </para>
    /// <para>
    /// Common test locations are available as static properties in <see cref="GeolocationCoordinates"/>:
    /// - GeolocationCoordinates.SanFrancisco
    /// - GeolocationCoordinates.NewYork
    /// - GeolocationCoordinates.London
    /// - etc.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if latitude is not between -90 and 90, or longitude is not between -180 and 180,
    /// or accuracy is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    Task SetGeolocationAsync(double latitude, double longitude, double? accuracy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the timezone for the browser context.
    /// </summary>
    /// <param name="timezoneId">
    /// The timezone ID (e.g., "America/New_York", "Europe/London", "Asia/Tokyo").
    /// Must be a valid IANA timezone identifier.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method changes the timezone that JavaScript Date functions will use.
    /// Useful for testing time-sensitive features or multi-timezone applications.
    /// </para>
    /// <para>
    /// The timezone affects:
    /// - new Date().toString()
    /// - Date.prototype.toLocaleString()
    /// - Intl.DateTimeFormat
    /// </para>
    /// <para>
    /// This setting persists for the entire browser context until changed.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="timezoneId"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="timezoneId"/> is not a valid timezone ID.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    Task SetTimezoneAsync(string timezoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the locale for the browser context.
    /// </summary>
    /// <param name="locale">
    /// The locale string (e.g., "en-US", "fr-FR", "ja-JP").
    /// Must be a valid BCP 47 language tag.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method sets the language and regional preferences for the browser.
    /// Affects the Accept-Language header and navigator.language values.
    /// </para>
    /// <para>
    /// The locale affects:
    /// - HTTP Accept-Language header
    /// - navigator.language
    /// - navigator.languages
    /// - Intl API formatting
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="locale"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    Task SetLocaleAsync(string locale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants specified permissions to the browser context.
    /// </summary>
    /// <param name="permissions">
    /// Array of permission names to grant (e.g., "geolocation", "notifications", "camera", "microphone").
    /// See Playwright documentation for full list of supported permissions.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method grants permissions that would normally require user interaction.
    /// Useful for automated testing of features that require permissions.
    /// </para>
    /// <para>
    /// Common permissions:
    /// - "geolocation" - Required for Geolocation API
    /// - "notifications" - Required for Web Notifications API
    /// - "camera" - Required for getUserMedia() camera access
    /// - "microphone" - Required for getUserMedia() microphone access
    /// - "midi" - Required for Web MIDI API
    /// - "midi-sysex" - Required for Web MIDI API with sysex
    /// - "clipboard-read" - Required for reading clipboard
    /// - "clipboard-write" - Required for writing clipboard
    /// </para>
    /// <para>
    /// Permissions are granted for the entire browser context and persist until changed.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="permissions"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    Task GrantPermissionsAsync(string[] permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all granted permissions for the browser context.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method revokes all previously granted permissions.
    /// After calling this method, permission prompts will appear again (in non-headless mode)
    /// or permission-dependent features will fail gracefully.
    /// </para>
    /// <para>
    /// Useful for testing permission denial scenarios or resetting state between tests.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the browser agent is not initialized.</exception>
    Task ClearPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current device profile being emulated, if any.
    /// </summary>
    /// <returns>
    /// The currently active device profile, or null if no device emulation is active.
    /// </returns>
    /// <remarks>
    /// This property allows checking what device configuration is currently active.
    /// Returns null if the browser is running in desktop mode without emulation.
    /// </remarks>
    DeviceProfile? CurrentDevice { get; }
}
