using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Execution;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Agents.Services.Execution;

/// <summary>
/// Service for collecting and processing test execution results
/// </summary>
public sealed class TestResultCollectorService : ITestResultCollector
{
    private readonly ILogger<TestResultCollectorService> _logger;
    private readonly Dictionary<Guid, TestExecutionSession> _activeSessions = new();
    private readonly object _lock = new();

    public TestResultCollectorService(ILogger<TestResultCollectorService> logger)
    {
        _logger = logger;
    }

    public Task<TestExecutionSession> StartSessionAsync(
        Guid recordingSessionId,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting execution session for recording {RecordingId}",
            recordingSessionId);

        var session = new TestExecutionSession
        {
            RecordingSessionId = recordingSessionId,
            TestFramework = metadata?.GetValueOrDefault("Framework") ?? "xUnit",
            Status = TestExecutionStatus.Pending,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        lock (_lock)
        {
            _activeSessions[session.Id] = session;
        }

        _logger.LogInformation("Created execution session {SessionId}", session.Id);

        return Task.FromResult(session);
    }

    public Task RecordStepResultAsync(
        Guid sessionId,
        TestStepResult stepResult,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Recording step result for session {SessionId}: Step {StepNumber} - {Status}",
            sessionId,
            stepResult.StepNumber,
            stepResult.Status);

        lock (_lock)
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                throw new InvalidOperationException($"Session {sessionId} not found");
            }

            session.StepResults.Add(stepResult);

            // Update session status if step failed
            if (stepResult.Status == TestExecutionStatus.Failed && session.Status != TestExecutionStatus.Failed)
            {
                session.Status = TestExecutionStatus.Failed;
                session.ErrorMessage = stepResult.ErrorMessage;
            }
        }

        return Task.CompletedTask;
    }

    public Task<TestExecutionResult> CompleteSessionAsync(
        Guid sessionId,
        TestExecutionStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Completing execution session {SessionId} with status {Status}",
            sessionId,
            status);

        lock (_lock)
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                throw new InvalidOperationException($"Session {sessionId} not found");
            }

            session.Status = status;
            session.CompletedAt = DateTimeOffset.UtcNow;
            session.ErrorMessage = errorMessage;

            // Convert session to final result
            var result = session.ToResult("GeneratedTest");

            // Remove from active sessions
            _activeSessions.Remove(sessionId);

            _logger.LogInformation(
                "Session {SessionId} completed: {PassedSteps}/{TotalSteps} steps passed",
                sessionId,
                result.PassedSteps,
                result.TotalSteps);

            return Task.FromResult(result);
        }
    }

    public Task AttachArtifactAsync(
        Guid sessionId,
        string artifactType,
        string artifactPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Attaching artifact to session {SessionId}: {Type} at {Path}",
            sessionId,
            artifactType,
            artifactPath);

        lock (_lock)
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                throw new InvalidOperationException($"Session {sessionId} not found");
            }

            var artifact = new TestArtifact
            {
                Type = artifactType,
                Path = artifactPath,
                FileName = Path.GetFileName(artifactPath),
                FileSizeBytes = File.Exists(artifactPath) ? new FileInfo(artifactPath).Length : 0,
                MimeType = GetMimeType(artifactType, artifactPath),
                StepNumber = session.CurrentStep > 0 ? session.CurrentStep : null
            };

            session.Artifacts.Add(artifact);
        }

        return Task.CompletedTask;
    }

    public Task RecordConsoleOutputAsync(
        Guid sessionId,
        string output,
        bool isError = false,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                throw new InvalidOperationException($"Session {sessionId} not found");
            }

            if (isError)
            {
                session.ErrorOutput.Add(output);
            }
            else
            {
                session.ConsoleOutput.Add(output);
            }
        }

        return Task.CompletedTask;
    }

    public Task<TestExecutionSession?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _activeSessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }
    }

    private static string? GetMimeType(string artifactType, string filePath)
    {
        return artifactType.ToLowerInvariant() switch
        {
            "screenshot" => "image/png",
            "video" => "video/webm",
            "log" => "text/plain",
            "trace" => "application/octet-stream",
            _ => Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webm" => "video/webm",
                ".mp4" => "video/mp4",
                ".txt" => "text/plain",
                ".log" => "text/plain",
                ".json" => "application/json",
                _ => "application/octet-stream"
            }
        };
    }
}
