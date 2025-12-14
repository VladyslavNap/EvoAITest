namespace EvoAITest.Core.Abstractions;

using EvoAITest.Core.Models;

/// <summary>
/// Interface for network interception and mocking capabilities.
/// Allows intercepting, blocking, and mocking HTTP requests during browser automation.
/// </summary>
public interface INetworkInterceptor : IAsyncDisposable
{
    /// <summary>
    /// Intercepts requests matching the specified pattern and allows custom handling.
    /// </summary>
    /// <param name="pattern">URL pattern to match (glob or regex).</param>
    /// <param name="handler">Handler function that receives the request and can return a custom response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// await interceptor.InterceptRequestAsync("**/api/users", async (request) =>
    /// {
    ///     if (request.Method == "GET")
    ///     {
    ///         return new InterceptedResponse
    ///         {
    ///             Status = 200,
    ///             Body = "{\"users\": []}"
    ///         };
    ///     }
    ///     return null; // Continue with normal request
    /// });
    /// </code>
    /// </example>
    Task InterceptRequestAsync(
        string pattern,
        Func<InterceptedRequest, Task<InterceptedResponse?>> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Blocks all requests matching the specified pattern.
    /// </summary>
    /// <param name="pattern">URL pattern to block (e.g., "*.jpg", "**/ads/**").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// // Block all image requests
    /// await interceptor.BlockRequestAsync("**/*.{png,jpg,jpeg,gif}");
    /// 
    /// // Block analytics
    /// await interceptor.BlockRequestAsync("**/analytics/**");
    /// </code>
    /// </example>
    Task BlockRequestAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mocks responses for requests matching the specified pattern.
    /// </summary>
    /// <param name="pattern">URL pattern to match.</param>
    /// <param name="response">Mock response to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// await interceptor.MockResponseAsync("**/api/config", new MockResponse
    /// {
    ///     Status = 200,
    ///     Body = "{\"feature_enabled\": true}",
    ///     ContentType = "application/json",
    ///     DelayMs = 100 // Simulate network latency
    /// });
    /// </code>
    /// </example>
    Task MockResponseAsync(
        string pattern,
        MockResponse response,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all network logs captured since logging was enabled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the list of network logs.</returns>
    Task<List<NetworkLog>> GetNetworkLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all network logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearNetworkLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all route interceptions (blocks and mocks).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearInterceptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables network logging.
    /// </summary>
    /// <param name="enabled">Whether to enable logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetNetworkLoggingAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether network logging is currently enabled.
    /// </summary>
    bool IsNetworkLoggingEnabled { get; }
}
