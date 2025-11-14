using EvoAITest.Core.Models;

namespace EvoAITest.Core.Options;

/// <summary>
/// Options for page navigation operations.
/// </summary>
public sealed class NavigationOptions
{
    /// <summary>
    /// Gets or sets the timeout for navigation in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the desired load state to wait for.
    /// </summary>
    public LoadState WaitUntil { get; set; } = LoadState.Load;

    /// <summary>
    /// Gets or sets the HTTP referer header.
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to wait for network idle before completing.
    /// </summary>
    public bool WaitForNetworkIdle { get; set; }

    /// <summary>
    /// Gets or sets the network idle timeout in milliseconds.
    /// </summary>
    public int NetworkIdleTimeoutMs { get; set; } = 500;
}
