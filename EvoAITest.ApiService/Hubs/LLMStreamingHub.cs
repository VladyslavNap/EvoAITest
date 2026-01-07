using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.AspNetCore.SignalR;

namespace EvoAITest.ApiService.Hubs;

/// <summary>
/// SignalR hub for streaming LLM responses to connected clients in real-time.
/// </summary>
/// <remarks>
/// <para>
/// This hub enables real-time, bi-directional communication between the server and clients
/// for streaming LLM completions. Clients can request completions and receive tokens as they
/// are generated, providing a responsive user experience.
/// </para>
/// <para>
/// The hub supports:
/// - Streaming completions with token-by-token delivery
/// - Client connection/disconnection handling
/// - Error propagation to clients
/// - Cancellation via connection termination
/// </para>
/// </remarks>
public sealed class LLMStreamingHub : Hub
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<LLMStreamingHub> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMStreamingHub"/> class.
    /// </summary>
    /// <param name="llmProvider">The LLM provider for generating completions.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    public LLMStreamingHub(
        ILLMProvider llmProvider,
        ILogger<LLMStreamingHub> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Streams an LLM completion to the calling client.
    /// </summary>
    /// <param name="request">The LLM request containing messages and options.</param>
    /// <param name="cancellationToken">Cancellation token tied to the client connection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method streams completion chunks to the client as they are generated.
    /// Each chunk is sent via the "ReceiveToken" method on the client side.
    /// </para>
    /// <para>
    /// The streaming will automatically stop if:
    /// - The completion finishes naturally
    /// - An error occurs (client receives error via "ReceiveError")
    /// - The client disconnects (cancellation token triggers)
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
    public async Task StreamCompletion(LLMRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var connectionId = Context.ConnectionId;
        _logger.LogInformation(
            "Starting streaming completion for connection {ConnectionId} with {MessageCount} messages",
            connectionId,
            request.Messages.Count);

        try
        {
            await foreach (var chunk in _llmProvider.StreamCompleteAsync(request, cancellationToken))
            {
                // Send each token to the calling client
                await Clients.Caller.SendAsync("ReceiveToken", chunk, cancellationToken);

                if (chunk.IsComplete)
                {
                    _logger.LogInformation(
                        "Streaming completed for connection {ConnectionId}, reason: {FinishReason}",
                        connectionId,
                        chunk.FinishReason);
                }
            }

            // Notify client that streaming is complete
            await Clients.Caller.SendAsync("StreamComplete", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Streaming cancelled for connection {ConnectionId}",
                connectionId);
            
            // Client will receive cancellation automatically
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during streaming completion for connection {ConnectionId}",
                connectionId);

            // Send error to client
            await Clients.Caller.SendAsync(
                "ReceiveError",
                new { message = ex.Message, type = ex.GetType().Name },
                cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Client connected: {ConnectionId}",
            Context.ConnectionId);

        return base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected: {ConnectionId}",
                Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
