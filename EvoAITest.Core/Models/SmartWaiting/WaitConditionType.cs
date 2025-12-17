namespace EvoAITest.Core.Models.SmartWaiting;

/// <summary>
/// Defines the type of wait condition to check.
/// </summary>
public enum WaitConditionType
{
    /// <summary>
    /// No specific condition.
    /// </summary>
    None = 0,

    /// <summary>
    /// Wait for network to be idle (no active requests).
    /// </summary>
    NetworkIdle = 1,

    /// <summary>
    /// Wait for DOM mutations to stop.
    /// </summary>
    DomStable = 2,

    /// <summary>
    /// Wait for CSS animations to complete.
    /// </summary>
    AnimationsComplete = 3,

    /// <summary>
    /// Wait for loading spinners/indicators to disappear.
    /// </summary>
    LoadersHidden = 4,

    /// <summary>
    /// Wait for JavaScript execution to complete.
    /// </summary>
    JavaScriptIdle = 5,

    /// <summary>
    /// Wait for images to finish loading.
    /// </summary>
    ImagesLoaded = 6,

    /// <summary>
    /// Wait for fonts to be loaded.
    /// </summary>
    FontsLoaded = 7,

    /// <summary>
    /// Wait for a custom predicate to return true.
    /// </summary>
    CustomPredicate = 8,

    /// <summary>
    /// Wait for page load event to fire.
    /// </summary>
    PageLoad = 9,

    /// <summary>
    /// Wait for DOM content loaded event.
    /// </summary>
    DomContentLoaded = 10
}
