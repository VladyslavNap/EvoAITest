using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Models;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Recording;
using EvoAITest.Core.Options;
using EvoAITest.Core.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvoAITest.Agents.Agents;

/// <summary>
/// Agent responsible for recording user interactions, analyzing them with AI,
/// and generating automated test code
/// </summary>
public sealed class RecordingAgent : IAgent
{
    private readonly IRecordingService _recordingService;
    private readonly IActionAnalyzer _actionAnalyzer;
    private readonly ITestGenerator _testGenerator;
    private readonly IRecordingRepository _repository;
    private readonly ILogger<RecordingAgent> _logger;
    private readonly RecordingOptions _options;

    public string Id => "recording-agent";
    public string Name => "Recording Agent";

    public AgentCapabilities Capabilities => new()
    {
        CanFillForms = false,
        CanNavigate = false,
        CanExtractData = false,
        SupportedBrowsers = []
    };

    public RecordingAgent(
        IRecordingService recordingService,
        IActionAnalyzer actionAnalyzer,
        ITestGenerator testGenerator,
        IRecordingRepository repository,
        ILogger<RecordingAgent> logger,
        IOptions<RecordingOptions> options)
    {
        _recordingService = recordingService;
        _actionAnalyzer = actionAnalyzer;
        _testGenerator = testGenerator;
        _repository = repository;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<AgentTaskResult> ExecuteTaskAsync(
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RecordingAgent executing task: {TaskId}", task.Id);

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Determine operation from task description
            var operation = ParseRecordingOperation(task.Description);

            object? result = operation switch
            {
                RecordingOperation.StartRecording => await StartRecordingAsync(task, cancellationToken),
                RecordingOperation.StopRecording => await StopRecordingAsync(task, cancellationToken),
                RecordingOperation.AnalyzeSession => await AnalyzeSessionAsync(task, cancellationToken),
                RecordingOperation.GenerateTest => await GenerateTestAsync(task, cancellationToken),
                RecordingOperation.ValidateAccuracy => await ValidateAccuracyAsync(task, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown recording operation: {operation}")
            };

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            return new AgentTaskResult
            {
                TaskId = task.Id,
                Success = true,
                DurationMs = (long)duration,
                ExtractedData = new Dictionary<string, object> { ["Result"] = result ?? "Success" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecordingAgent failed to execute task: {TaskId}", task.Id);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            return new AgentTaskResult
            {
                TaskId = task.Id,
                Success = false,
                ErrorMessage = ex.Message,
                Error = ex,
                DurationMs = (long)duration
            };
        }
    }

    public Task<IReadOnlyList<AgentStep>> PlanTaskAsync(
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RecordingAgent planning task: {TaskId}", task.Id);

        var steps = new List<AgentStep>
        {
            new AgentStep
            {
                StepNumber = 1,
                Reasoning = "Execute recording operation"
            }
        };

        return Task.FromResult<IReadOnlyList<AgentStep>>(steps.AsReadOnly());
    }

    public Task LearnFromFeedbackAsync(
        AgentFeedback feedback,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RecordingAgent learning from feedback");

        // TODO: Implement learning from feedback
        return Task.CompletedTask;
    }

    #region Private Methods

    private RecordingOperation ParseRecordingOperation(string description)
    {
        var lower = description?.ToLowerInvariant() ?? string.Empty;

        if (lower.Contains("start") && lower.Contains("record"))
            return RecordingOperation.StartRecording;
        if (lower.Contains("stop") && lower.Contains("record"))
            return RecordingOperation.StopRecording;
        if (lower.Contains("analyze"))
            return RecordingOperation.AnalyzeSession;
        if (lower.Contains("generate") && lower.Contains("test"))
            return RecordingOperation.GenerateTest;
        if (lower.Contains("validate") || lower.Contains("accuracy"))
            return RecordingOperation.ValidateAccuracy;

        return RecordingOperation.Unknown;
    }

    private async Task<RecordingSession> StartRecordingAsync(
        AgentTask task,
        CancellationToken cancellationToken)
    {
        var name = task.Parameters.GetValueOrDefault("name", "New Recording")?.ToString() ?? "New Recording";
        var url = task.Parameters.GetValueOrDefault("url", "")?.ToString() ?? "";

        _logger.LogInformation("Starting recording: {Name} at {Url}", name, url);

        var configuration = new RecordingConfiguration
        {
            CaptureScreenshots = _options.CaptureScreenshots,
            RecordNetwork = _options.RecordNetworkTraffic,
            RecordConsoleLogs = _options.RecordConsoleLogs,
            AutoGenerateAssertions = _options.AutoGenerateAssertions,
            UseAiIntentDetection = _options.UseAiAnalysis
        };

        var session = await _recordingService.StartRecordingAsync(name, url, configuration, cancellationToken);
        await _repository.CreateSessionAsync(session, cancellationToken);

        return session;
    }

    private async Task<RecordingSession> StopRecordingAsync(
        AgentTask task,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.Parse(task.Parameters.GetValueOrDefault("sessionId", Guid.Empty)?.ToString() ?? Guid.Empty.ToString());

        _logger.LogInformation("Stopping recording: {SessionId}", sessionId);

        var session = await _recordingService.StopRecordingAsync(sessionId, cancellationToken);
        await _repository.UpdateSessionAsync(session, cancellationToken);

        return session;
    }

    private async Task<RecordingSession> AnalyzeSessionAsync(
        AgentTask task,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.Parse(task.Parameters.GetValueOrDefault("sessionId", Guid.Empty)?.ToString() ?? Guid.Empty.ToString());

        _logger.LogInformation("Analyzing recording session: {SessionId}", sessionId);

        var session = await _repository.GetSessionByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        var unknownInteractions = session.Interactions
            .Where(i => i.Intent == ActionIntent.Unknown)
            .ToList();

        foreach (var interaction in unknownInteractions)
        {
            var analyzed = await _actionAnalyzer.AnalyzeInteractionAsync(
                interaction,
                session,
                cancellationToken);

            interaction.Intent = analyzed.Intent;
            interaction.IntentConfidence = analyzed.IntentConfidence;
            interaction.Description = analyzed.Description;

            await _repository.UpdateInteractionAsync(interaction, cancellationToken);
        }

        session.Metrics.AverageIntentConfidence = session.Interactions.Any()
            ? session.Interactions.Average(i => i.IntentConfidence)
            : 0.0;

        await _repository.UpdateSessionAsync(session, cancellationToken);

        return session;
    }

    private async Task<GeneratedTest> GenerateTestAsync(
        AgentTask task,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.Parse(task.Parameters.GetValueOrDefault("sessionId", Guid.Empty)?.ToString() ?? Guid.Empty.ToString());
        var framework = task.Parameters.GetValueOrDefault("framework", _options.DefaultTestFramework)?.ToString()
            ?? _options.DefaultTestFramework;

        _logger.LogInformation("Generating test for session: {SessionId}", sessionId);

        var session = await _repository.GetSessionByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        var options = new TestGenerationOptions
        {
            TestFramework = framework,
            Language = _options.DefaultLanguage,
            AutomationLibrary = _options.DefaultAutomationLibrary,
            IncludeComments = true,
            AutoGenerateAssertions = _options.AutoGenerateAssertions,
            MinimumConfidenceThreshold = _options.MinimumConfidenceThreshold
        };

        var generatedTest = await _testGenerator.GenerateTestAsync(session, options, cancellationToken);

        session.GeneratedTestCode = generatedTest.Code;
        session.Status = RecordingStatus.Generated;
        session.Metrics.AssertionsGenerated = generatedTest.Metrics.AssertionCount;

        await _repository.UpdateSessionAsync(session, cancellationToken);

        return generatedTest;
    }

    private async Task<ActionRecognitionMetrics> ValidateAccuracyAsync(
        AgentTask task,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.Parse(task.Parameters.GetValueOrDefault("sessionId", Guid.Empty)?.ToString() ?? Guid.Empty.ToString());

        _logger.LogInformation("Validating accuracy for session: {SessionId}", sessionId);

        var session = await _repository.GetSessionByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        var metrics = await _actionAnalyzer.ValidateActionRecognitionAsync(session, cancellationToken);

        session.Metrics.ActionRecognitionAccuracy = metrics.AccuracyPercentage;
        session.Metrics.AverageIntentConfidence = metrics.AverageConfidence;

        await _repository.UpdateSessionAsync(session, cancellationToken);

        if (metrics.AccuracyPercentage < _options.TargetAccuracyPercentage)
        {
            _logger.LogWarning(
                "Session {SessionId} accuracy {Accuracy:P0} is below target {Target:P0}",
                sessionId,
                metrics.AccuracyPercentage / 100,
                _options.TargetAccuracyPercentage / 100);
        }

        return metrics;
    }

    #endregion
}

/// <summary>
/// Types of recording operations
/// </summary>
internal enum RecordingOperation
{
    Unknown,
    StartRecording,
    StopRecording,
    AnalyzeSession,
    GenerateTest,
    ValidateAccuracy
}
