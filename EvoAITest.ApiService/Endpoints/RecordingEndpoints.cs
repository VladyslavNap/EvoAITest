using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Recording;
using EvoAITest.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EvoAITest.ApiService.Endpoints;

/// <summary>
/// Extension methods for registering recording endpoints.
/// </summary>
public static class RecordingEndpoints
{
    /// <summary>
    /// Maps all recording endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapRecordingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recordings")
            .WithTags("Recordings")
            .WithOpenApi();

        // POST /api/recordings/start - Start recording
        group.MapPost("/start", StartRecording)
            .WithName("StartRecording")
            .WithSummary("Start a new recording session")
            .WithDescription("Starts a new recording session for test generation")
            .Produces<RecordingSession>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // POST /api/recordings/{id}/stop - Stop recording
        group.MapPost("/{id:guid}/stop", StopRecording)
            .WithName("StopRecording")
            .WithSummary("Stop a recording session")
            .WithDescription("Stops an active recording session")
            .Produces<RecordingSession>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/recordings/{id}/pause - Pause recording
        group.MapPost("/{id:guid}/pause", PauseRecording)
            .WithName("PauseRecording")
            .WithSummary("Pause a recording session")
            .WithDescription("Pauses an active recording session")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/recordings/{id}/resume - Resume recording
        group.MapPost("/{id:guid}/resume", ResumeRecording)
            .WithName("ResumeRecording")
            .WithSummary("Resume a paused recording session")
            .WithDescription("Resumes a paused recording session")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/recordings - Get all recordings
        group.MapGet("/", GetAllRecordings)
            .WithName("GetAllRecordings")
            .WithSummary("Get all recording sessions")
            .WithDescription("Retrieves all recording sessions, optionally filtered by status")
            .Produces<List<RecordingSession>>(StatusCodes.Status200OK);

        // GET /api/recordings/{id} - Get recording by ID
        group.MapGet("/{id:guid}", GetRecordingById)
            .WithName("GetRecordingById")
            .WithSummary("Get a specific recording by ID")
            .WithDescription("Retrieves a specific recording session by its unique identifier")
            .Produces<RecordingSession>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/recordings/{id}/interactions - Get interactions
        group.MapGet("/{id:guid}/interactions", GetInteractions)
            .WithName("GetInteractions")
            .WithSummary("Get interactions for a recording")
            .WithDescription("Retrieves all user interactions for a specific recording session")
            .Produces<List<UserInteraction>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/recordings/{id}/analyze - Analyze recording
        group.MapPost("/{id:guid}/analyze", AnalyzeRecording)
            .WithName("AnalyzeRecording")
            .WithSummary("Analyze a recording session with AI")
            .WithDescription("Analyzes user interactions to detect intent and generate descriptions")
            .Produces<RecordingSession>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/recordings/{id}/generate - Generate test
        group.MapPost("/{id:guid}/generate", GenerateTest)
            .WithName("GenerateTest")
            .WithSummary("Generate test code from recording")
            .WithDescription("Generates automated test code from a recording session")
            .Produces<GeneratedTest>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/recordings/{id}/validate - Validate accuracy
        group.MapPost("/{id:guid}/validate", ValidateAccuracy)
            .WithName("ValidateAccuracy")
            .WithSummary("Validate action recognition accuracy")
            .WithDescription("Validates the accuracy of action recognition in a recording")
            .Produces<ActionRecognitionMetrics>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/recordings/{id}/generate-stream - Stream test generation
        group.MapGet("/{id:guid}/generate-stream", StreamGenerateTest)
            .WithName("StreamGenerateTest")
            .WithSummary("Stream test generation in real-time")
            .WithDescription("Generates test code and streams the result token-by-token using Server-Sent Events (SSE)")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/recordings/{id}/analyze-stream - Stream analysis
        group.MapGet("/{id:guid}/analyze-stream", StreamAnalyzeRecording)
            .WithName("StreamAnalyzeRecording")
            .WithSummary("Stream recording analysis in real-time")
            .WithDescription("Analyzes interactions and streams intent detection results using Server-Sent Events (SSE)")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    #region Endpoint Handlers

    private static async Task<IResult> StartRecording(
        [FromBody] StartRecordingRequest request,
        IRecordingService recordingService,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting recording: {Name}", request.Name);

            var configuration = new RecordingConfiguration
            {
                CaptureScreenshots = request.CaptureScreenshots,
                RecordNetwork = request.RecordNetwork,
                RecordConsoleLogs = request.RecordConsoleLogs,
                AutoGenerateAssertions = request.AutoGenerateAssertions,
                UseAiIntentDetection = request.UseAiIntentDetection
            };

            var session = await recordingService.StartRecordingAsync(
                request.Name,
                request.StartUrl,
                configuration,
                cancellationToken);

            // Save to repository
            await repository.CreateSessionAsync(session, cancellationToken);

            return Results.Created($"/api/recordings/{session.Id}", session);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start recording");
            return Results.Problem(
                title: "Failed to start recording",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> StopRecording(
        Guid id,
        IRecordingService recordingService,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Stopping recording: {SessionId}", id);

            var session = await recordingService.StopRecordingAsync(id, cancellationToken);
            await repository.UpdateSessionAsync(session, cancellationToken);

            return Results.Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Recording not found: {SessionId}", id);
            return Results.NotFound();
        }
    }

    private static async Task<IResult> PauseRecording(
        Guid id,
        IRecordingService recordingService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Pausing recording: {SessionId}", id);
            await recordingService.PauseRecordingAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Recording not found: {SessionId}", id);
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ResumeRecording(
        Guid id,
        IRecordingService recordingService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Resuming recording: {SessionId}", id);
            await recordingService.ResumeRecordingAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Recording not found: {SessionId}", id);
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetAllRecordings(
        [FromQuery] string? status,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all recordings, status: {Status}", status ?? "all");

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<RecordingStatus>(status, true, out var recordingStatus))
        {
            var filteredSessions = await repository.GetSessionsByStatusAsync(recordingStatus, cancellationToken);
            return Results.Ok(filteredSessions);
        }

        var sessions = await repository.GetAllSessionsAsync(cancellationToken);
        return Results.Ok(sessions);
    }

    private static async Task<IResult> GetRecordingById(
        Guid id,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting recording: {SessionId}", id);

        var session = await repository.GetSessionByIdAsync(id, cancellationToken);

        return session != null
            ? Results.Ok(session)
            : Results.NotFound();
    }

    private static async Task<IResult> GetInteractions(
        Guid id,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting interactions for recording: {SessionId}", id);

        var interactions = await repository.GetSessionInteractionsAsync(id, cancellationToken);
        return Results.Ok(interactions);
    }

    private static async Task<IResult> AnalyzeRecording(
        Guid id,
        IActionAnalyzer actionAnalyzer,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Analyzing recording: {SessionId}", id);

            var session = await repository.GetSessionByIdAsync(id, cancellationToken);
            if (session == null)
            {
                return Results.NotFound();
            }

            // Analyze each interaction
            var unknownInteractions = session.Interactions
                .Where(i => i.Intent == ActionIntent.Unknown)
                .ToList();

            foreach (var interaction in unknownInteractions)
            {
                var analyzed = await actionAnalyzer.AnalyzeInteractionAsync(
                    interaction,
                    session,
                    cancellationToken);

                interaction.Intent = analyzed.Intent;
                interaction.IntentConfidence = analyzed.IntentConfidence;
                interaction.Description = analyzed.Description;

                await repository.UpdateInteractionAsync(interaction, cancellationToken);
            }

            // Update session metrics
            session.Metrics.AverageIntentConfidence = session.Interactions.Any()
                ? session.Interactions.Average(i => i.IntentConfidence)
                : 0.0;

            await repository.UpdateSessionAsync(session, cancellationToken);

            return Results.Ok(session);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze recording: {SessionId}", id);
            return Results.Problem(
                title: "Failed to analyze recording",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GenerateTest(
        Guid id,
        [FromBody] GenerateTestRequest request,
        ITestGenerator testGenerator,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Generating test for recording: {SessionId}", id);

            var session = await repository.GetSessionByIdAsync(id, cancellationToken);
            if (session == null)
            {
                return Results.NotFound();
            }

            var options = new TestGenerationOptions
            {
                TestFramework = request.TestFramework,
                Language = request.Language,
                IncludeComments = request.IncludeComments,
                GeneratePageObjects = request.GeneratePageObjects,
                AutoGenerateAssertions = request.AutoGenerateAssertions,
                Namespace = request.Namespace,
                ClassName = request.ClassName
            };

            var generatedTest = await testGenerator.GenerateTestAsync(session, options, cancellationToken);

            // Update session
            session.GeneratedTestCode = generatedTest.Code;
            session.Status = RecordingStatus.Generated;
            session.Metrics.AssertionsGenerated = generatedTest.Metrics.AssertionCount;

            await repository.UpdateSessionAsync(session, cancellationToken);

            return Results.Ok(generatedTest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate test for recording: {SessionId}", id);
            return Results.Problem(
                title: "Failed to generate test",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ValidateAccuracy(
        Guid id,
        IActionAnalyzer actionAnalyzer,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Validating accuracy for recording: {SessionId}", id);

            var session = await repository.GetSessionByIdAsync(id, cancellationToken);
            if (session == null)
            {
                return Results.NotFound();
            }

            var metrics = await actionAnalyzer.ValidateActionRecognitionAsync(session, cancellationToken);

            // Update session metrics
            session.Metrics.ActionRecognitionAccuracy = metrics.AccuracyPercentage;
            session.Metrics.AverageIntentConfidence = metrics.AverageConfidence;

            await repository.UpdateSessionAsync(session, cancellationToken);

            return Results.Ok(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate accuracy for recording: {SessionId}", id);
            return Results.Problem(
                title: "Failed to validate accuracy",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> DeleteRecording(
        Guid id,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting recording: {SessionId}", id);

        await repository.DeleteSessionAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetRecentRecordings(
        IRecordingRepository repository,
        ILogger<Program> logger,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting {Count} recent recordings", count);

        var sessions = await repository.GetRecentSessionsAsync(count, cancellationToken);
        return Results.Ok(sessions);
    }

    /// <summary>
    /// Streams test generation in real-time using Server-Sent Events (SSE).
    /// </summary>
    private static async Task StreamGenerateTest(
        Guid id,
        HttpContext context,
        ITestGenerator testGenerator,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting streaming test generation for recording: {SessionId}", id);

            var session = await repository.GetSessionByIdAsync(id, cancellationToken);
            if (session == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = "Recording not found" }, cancellationToken);
                return;
            }

            // Set SSE headers
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            var options = new TestGenerationOptions
            {
                TestFramework = "Playwright",
                Language = "C#",
                IncludeComments = true,
                GeneratePageObjects = false,
                AutoGenerateAssertions = true
            };

            // Send start event
            await context.Response.WriteAsync("data: {\"status\": \"started\"}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);

            // Generate test (non-streaming version)
            var generatedTest = await testGenerator.GenerateTestAsync(session, options, cancellationToken);

            // Stream the generated code in chunks for demonstration
            var codeLines = generatedTest.Code.Split('\n');
            foreach (var line in codeLines)
            {
                var chunk = new { type = "code", content = line + "\n" };
                await context.Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(chunk)}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
                await Task.Delay(50, cancellationToken); // Simulate streaming delay
            }

            // Send completion event
            await context.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);

            logger.LogInformation("Completed streaming test generation for recording: {SessionId}", id);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Streaming test generation cancelled for recording: {SessionId}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during streaming test generation for recording: {SessionId}", id);
            
            // Send error event
            await context.Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Streams recording analysis in real-time using Server-Sent Events (SSE).
    /// </summary>
    private static async Task StreamAnalyzeRecording(
        Guid id,
        HttpContext context,
        IActionAnalyzer actionAnalyzer,
        IRecordingRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting streaming analysis for recording: {SessionId}", id);

            var session = await repository.GetSessionByIdAsync(id, cancellationToken);
            if (session == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = "Recording not found" }, cancellationToken);
                return;
            }

            // Set SSE headers
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            var unknownInteractions = session.Interactions
                .Where(i => i.Intent == ActionIntent.Unknown)
                .ToList();

            foreach (var interaction in unknownInteractions)
            {
                // Send progress event
                await context.Response.WriteAsync(
                    $"data: {{\"status\": \"analyzing\", \"interactionId\": \"{interaction.Id}\"}}\n\n",
                    cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);

                var analyzed = await actionAnalyzer.AnalyzeInteractionAsync(
                    interaction,
                    session,
                    cancellationToken);

                // Send result event
                var result = new
                {
                    interactionId = interaction.Id,
                    intent = analyzed.Intent.ToString(),
                    confidence = analyzed.IntentConfidence,
                    description = analyzed.Description
                };

                await context.Response.WriteAsJsonAsync(result, cancellationToken);
                await context.Response.WriteAsync("\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);

                // Update in database
                interaction.Intent = analyzed.Intent;
                interaction.IntentConfidence = analyzed.IntentConfidence;
                interaction.Description = analyzed.Description;
                await repository.UpdateInteractionAsync(interaction, cancellationToken);
            }

            // Send completion event
            await context.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);

            logger.LogInformation("Completed streaming analysis for recording: {SessionId}", id);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Streaming analysis cancelled for recording: {SessionId}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during streaming analysis for recording: {SessionId}", id);
            
            // Send error event
            await context.Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    #endregion
}

#region Request Models

/// <summary>
/// Request to start a new recording session
/// </summary>
public sealed class StartRecordingRequest
{
    public required string Name { get; init; }
    public required string StartUrl { get; init; }
    public string? Description { get; init; }
    public bool CaptureScreenshots { get; init; } = true;
    public bool RecordNetwork { get; init; } = false;
    public bool RecordConsoleLogs { get; init; } = false;
    public bool AutoGenerateAssertions { get; init; } = true;
    public bool UseAiIntentDetection { get; init; } = true;
}

/// <summary>
/// Request to generate test code from a recording
/// </summary>
public sealed class GenerateTestRequest
{
    public string TestFramework { get; init; } = "xUnit";
    public string Language { get; init; } = "C#";
    public bool IncludeComments { get; init; } = true;
    public bool GeneratePageObjects { get; init; } = false;
    public bool AutoGenerateAssertions { get; init; } = true;
    public string Namespace { get; init; } = "EvoAITest.Generated";
    public string? ClassName { get; init; }
}

#endregion
