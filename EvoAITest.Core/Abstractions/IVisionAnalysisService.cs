using EvoAITest.Core.Models.Vision;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for analyzing screenshots using AI vision models to detect and locate UI elements.
/// Supports GPT-4 Vision and Azure Computer Vision providers.
/// </summary>
public interface IVisionAnalysisService
{
    /// <summary>
    /// Detects UI elements in a screenshot.
    /// </summary>
    /// <param name="screenshot">The screenshot as a byte array (PNG or JPEG).</param>
    /// <param name="filter">Optional filter to narrow down results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of detected elements.</returns>
    /// <example>
    /// <code>
    /// var screenshot = await browserAgent.TakeFullPageScreenshotBytesAsync();
    /// var filter = ElementFilter.ForButtons(minConfidence: 0.8);
    /// var result = await visionService.DetectElementsAsync(screenshot, filter);
    /// 
    /// foreach (var element in result.Elements.Where(e => e.IsReliable()))
    /// {
    ///     Console.WriteLine(element.GetDisplayDescription());
    /// }
    /// </code>
    /// </example>
    Task<VisionAnalysisResult> DetectElementsAsync(
        byte[] screenshot,
        ElementFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a specific element by natural language description.
    /// </summary>
    /// <param name="screenshot">The screenshot to analyze.</param>
    /// <param name="description">Natural language description (e.g., "the blue submit button on the right").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best matching element or null if not found.</returns>
    /// <example>
    /// <code>
    /// var element = await visionService.FindElementByDescriptionAsync(
    ///     screenshot,
    ///     "the login button at the top right",
    ///     cancellationToken);
    ///     
    /// if (element != null && element.IsReliable())
    /// {
    ///     await browserAgent.ClickAsync(element.SuggestedSelector!);
    /// }
    /// </code>
    /// </example>
    Task<DetectedElement?> FindElementByDescriptionAsync(
        byte[] screenshot,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts text from a screenshot using OCR.
    /// </summary>
    /// <param name="screenshot">The screenshot to analyze.</param>
    /// <param name="region">Optional region to extract text from (null = entire screenshot).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted text or empty string if no text found.</returns>
    /// <example>
    /// <code>
    /// var text = await visionService.ExtractTextAsync(screenshot);
    /// Console.WriteLine($"Page text: {text}");
    /// 
    /// // Extract text from specific region
    /// var region = new ElementBoundingBox { X = 100, Y = 100, Width = 200, Height = 50 };
    /// var headerText = await visionService.ExtractTextAsync(screenshot, region);
    /// </code>
    /// </example>
    Task<string?> ExtractTextAsync(
        byte[] screenshot,
        ElementBoundingBox? region = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Locates an element by description and returns its bounding box.
    /// </summary>
    /// <param name="screenshot">The screenshot to analyze.</param>
    /// <param name="elementDescription">Description of the element to locate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bounding box of the element.</returns>
    /// <exception cref="InvalidOperationException">If element not found.</exception>
    Task<ElementBoundingBox> LocateElementAsync(
        byte[] screenshot,
        string elementDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a screenshot and provides general observations about the UI.
    /// </summary>
    /// <param name="screenshot">The screenshot to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Human-readable description of what's visible in the screenshot.</returns>
    /// <example>
    /// <code>
    /// var observations = await visionService.AnalyzeScreenshotAsync(screenshot);
    /// Console.WriteLine($"Screenshot contains: {observations}");
    /// // Output: "Screenshot contains: A login form with username and password fields, 
    /// //          a blue submit button, and a 'Forgot password?' link at the bottom"
    /// </code>
    /// </example>
    Task<string> AnalyzeScreenshotAsync(
        byte[] screenshot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a CSS selector for a detected element based on its visual properties.
    /// </summary>
    /// <param name="element">The detected element.</param>
    /// <param name="pageState">Optional page state for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A CSS selector that should uniquely identify the element.</returns>
    Task<string> GenerateSelectorAsync(
        DetectedElement element,
        Models.PageState? pageState = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that an element at a specific location matches expectations.
    /// </summary>
    /// <param name="screenshot">The screenshot containing the element.</param>
    /// <param name="boundingBox">The expected location of the element.</param>
    /// <param name="expectedType">The expected element type.</param>
    /// <param name="expectedText">Optional expected text content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the element matches expectations, false otherwise.</returns>
    Task<bool> VerifyElementAsync(
        byte[] screenshot,
        ElementBoundingBox boundingBox,
        ElementType expectedType,
        string? expectedText = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two screenshots and identifies elements that have changed.
    /// </summary>
    /// <param name="beforeScreenshot">Screenshot before the change.</param>
    /// <param name="afterScreenshot">Screenshot after the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of elements that changed between screenshots.</returns>
    Task<List<DetectedElement>> FindChangedElementsAsync(
        byte[] beforeScreenshot,
        byte[] afterScreenshot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the vision provider currently in use (GPT4Vision, AzureCV, etc.).
    /// </summary>
    string CurrentProvider { get; }

    /// <summary>
    /// Gets whether OCR capabilities are available.
    /// </summary>
    bool SupportsOCR { get; }

    /// <summary>
    /// Gets whether element detection is available.
    /// </summary>
    bool SupportsElementDetection { get; }
}
