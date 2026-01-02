using EvoAITest.Core.Data;
using EvoAITest.Core.Data.Models;
using EvoAITest.Core.Models.Recording;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EvoAITest.Core.Repositories;

/// <summary>
/// Repository for recording session persistence using Entity Framework Core
/// </summary>
public sealed class RecordingRepository : IRecordingRepository
{
    private readonly EvoAIDbContext _context;
    private readonly ILogger<RecordingRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public RecordingRepository(
        EvoAIDbContext context,
        ILogger<RecordingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RecordingSession> CreateSessionAsync(
        RecordingSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating recording session: {SessionId}", session.Id);

        var entity = MapToEntity(session);
        _context.RecordingSessions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created recording session: {SessionId}", session.Id);
        return session;
    }

    public async Task<RecordingSession> UpdateSessionAsync(
        RecordingSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating recording session: {SessionId}", session.Id);

        var entity = await _context.RecordingSessions
            .Include(s => s.Interactions)
            .FirstOrDefaultAsync(s => s.Id == session.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Recording session {session.Id} not found");
        }

        UpdateEntity(entity, session);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated recording session: {SessionId}", session.Id);
        return session;
    }

    public async Task<RecordingSession?> GetSessionByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting recording session: {SessionId}", sessionId);

        var entity = await _context.RecordingSessions
            .Include(s => s.Interactions.OrderBy(i => i.SequenceNumber))
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        return entity != null ? MapFromEntity(entity) : null;
    }

    public async Task<List<RecordingSession>> GetAllSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all recording sessions");

        var entities = await _context.RecordingSessions
            .Include(s => s.Interactions.OrderBy(i => i.SequenceNumber))
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapFromEntity).ToList();
    }

    public async Task<List<RecordingSession>> GetSessionsByStatusAsync(
        RecordingStatus status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting recording sessions by status: {Status}", status);

        var statusString = status.ToString();
        var entities = await _context.RecordingSessions
            .Include(s => s.Interactions.OrderBy(i => i.SequenceNumber))
            .Where(s => s.Status == statusString)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapFromEntity).ToList();
    }

    public async Task<List<RecordingSession>> GetSessionsByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting recording sessions for user: {UserId}", userId);

        var entities = await _context.RecordingSessions
            .Include(s => s.Interactions.OrderBy(i => i.SequenceNumber))
            .Where(s => s.CreatedBy == userId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapFromEntity).ToList();
    }

    public async Task<List<RecordingSession>> GetRecentSessionsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting {Count} recent recording sessions", count);

        var entities = await _context.RecordingSessions
            .Include(s => s.Interactions.OrderBy(i => i.SequenceNumber))
            .OrderByDescending(s => s.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

        return entities.Select(MapFromEntity).ToList();
    }

    public async Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting recording session: {SessionId}", sessionId);

        var entity = await _context.RecordingSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (entity != null)
        {
            _context.RecordingSessions.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted recording session: {SessionId}", sessionId);
        }
    }

    public async Task AddInteractionAsync(
        Guid sessionId,
        UserInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding interaction to session: {SessionId}", sessionId);

        var entity = MapInteractionToEntity(interaction);
        _context.RecordedInteractions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateInteractionAsync(
        UserInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating interaction: {InteractionId}", interaction.Id);

        var entity = await _context.RecordedInteractions
            .FirstOrDefaultAsync(i => i.Id == interaction.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Interaction {interaction.Id} not found");
        }

        UpdateInteractionEntity(entity, interaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UserInteraction>> GetSessionInteractionsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting interactions for session: {SessionId}", sessionId);

        var entities = await _context.RecordedInteractions
            .Where(i => i.SessionId == sessionId)
            .OrderBy(i => i.SequenceNumber)
            .ToListAsync(cancellationToken);

        return entities.Select(MapInteractionFromEntity).ToList();
    }

    public async Task<int> DeleteOldRecordingsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting recordings older than {Days} days", retentionDays);

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var oldSessions = await _context.RecordingSessions
            .Where(s => s.StartedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        var count = oldSessions.Count;
        if (count > 0)
        {
            _context.RecordingSessions.RemoveRange(oldSessions);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted {Count} old recording sessions", count);
        }

        return count;
    }

    #region Mapping Methods

    private RecordingSessionEntity MapToEntity(RecordingSession session)
    {
        return new RecordingSessionEntity
        {
            Id = session.Id,
            Name = session.Name,
            Description = session.Description,
            Status = session.Status.ToString(),
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            StartUrl = session.StartUrl,
            Browser = session.Browser,
            ViewportWidth = session.ViewportSize.Width,
            ViewportHeight = session.ViewportSize.Height,
            GeneratedTestCode = session.GeneratedTestCode,
            TestFramework = session.TestFramework,
            Language = session.Language,
            ConfigurationJson = JsonSerializer.Serialize(session.Configuration, JsonOptions),
            MetricsJson = JsonSerializer.Serialize(session.Metrics, JsonOptions),
            TagsJson = session.Tags.Any() ? JsonSerializer.Serialize(session.Tags, JsonOptions) : null,
            CreatedBy = session.CreatedBy,
            Interactions = session.Interactions.Select(MapInteractionToEntity).ToList()
        };
    }

    private void UpdateEntity(RecordingSessionEntity entity, RecordingSession session)
    {
        entity.Name = session.Name;
        entity.Description = session.Description;
        entity.Status = session.Status.ToString();
        entity.EndedAt = session.EndedAt;
        entity.GeneratedTestCode = session.GeneratedTestCode;
        entity.TestFramework = session.TestFramework;
        entity.Language = session.Language;
        entity.ConfigurationJson = JsonSerializer.Serialize(session.Configuration, JsonOptions);
        entity.MetricsJson = JsonSerializer.Serialize(session.Metrics, JsonOptions);
        entity.TagsJson = session.Tags.Any() ? JsonSerializer.Serialize(session.Tags, JsonOptions) : null;
    }

    private RecordingSession MapFromEntity(RecordingSessionEntity entity)
    {
        return new RecordingSession
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Status = Enum.Parse<RecordingStatus>(entity.Status),
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            StartUrl = entity.StartUrl,
            Browser = entity.Browser,
            ViewportSize = (entity.ViewportWidth, entity.ViewportHeight),
            GeneratedTestCode = entity.GeneratedTestCode,
            TestFramework = entity.TestFramework,
            Language = entity.Language,
            Configuration = JsonSerializer.Deserialize<RecordingConfiguration>(entity.ConfigurationJson, JsonOptions) ?? new(),
            Metrics = JsonSerializer.Deserialize<RecordingMetrics>(entity.MetricsJson, JsonOptions) ?? new(),
            Tags = string.IsNullOrEmpty(entity.TagsJson)
                ? []
                : JsonSerializer.Deserialize<List<string>>(entity.TagsJson, JsonOptions) ?? [],
            CreatedBy = entity.CreatedBy,
            Interactions = entity.Interactions.Select(MapInteractionFromEntity).ToList()
        };
    }

    private RecordedInteractionEntity MapInteractionToEntity(UserInteraction interaction)
    {
        return new RecordedInteractionEntity
        {
            Id = interaction.Id,
            SessionId = interaction.SessionId,
            SequenceNumber = interaction.SequenceNumber,
            ActionType = interaction.ActionType.ToString(),
            Intent = interaction.Intent.ToString(),
            Description = interaction.Description,
            Timestamp = interaction.Timestamp,
            DurationMs = interaction.DurationMs,
            InputValue = interaction.InputValue,
            Key = interaction.Key,
            CoordinateX = interaction.Coordinates?.X,
            CoordinateY = interaction.Coordinates?.Y,
            ContextJson = JsonSerializer.Serialize(interaction.Context, JsonOptions),
            IncludeInTest = interaction.IncludeInTest,
            IntentConfidence = interaction.IntentConfidence,
            GeneratedCode = interaction.GeneratedCode,
            AssertionsJson = interaction.Assertions.Any()
                ? JsonSerializer.Serialize(interaction.Assertions, JsonOptions)
                : null
        };
    }

    private void UpdateInteractionEntity(RecordedInteractionEntity entity, UserInteraction interaction)
    {
        entity.Intent = interaction.Intent.ToString();
        entity.Description = interaction.Description;
        entity.IncludeInTest = interaction.IncludeInTest;
        entity.IntentConfidence = interaction.IntentConfidence;
        entity.GeneratedCode = interaction.GeneratedCode;
        entity.AssertionsJson = interaction.Assertions.Any()
            ? JsonSerializer.Serialize(interaction.Assertions, JsonOptions)
            : null;
    }

    private UserInteraction MapInteractionFromEntity(RecordedInteractionEntity entity)
    {
        var context = JsonSerializer.Deserialize<ActionContext>(entity.ContextJson, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize context for interaction {entity.Id}");

        var assertions = string.IsNullOrEmpty(entity.AssertionsJson)
            ? new List<ActionAssertion>()
            : JsonSerializer.Deserialize<List<ActionAssertion>>(entity.AssertionsJson, JsonOptions) ?? [];

        (int X, int Y)? coordinates = entity.CoordinateX.HasValue && entity.CoordinateY.HasValue
            ? (entity.CoordinateX.Value, entity.CoordinateY.Value)
            : null;

        return new UserInteraction
        {
            Id = entity.Id,
            SessionId = entity.SessionId,
            SequenceNumber = entity.SequenceNumber,
            ActionType = Enum.Parse<ActionType>(entity.ActionType),
            Intent = Enum.Parse<ActionIntent>(entity.Intent),
            Description = entity.Description,
            Timestamp = entity.Timestamp,
            DurationMs = entity.DurationMs,
            InputValue = entity.InputValue,
            Key = entity.Key,
            Coordinates = coordinates,
            Context = context,
            IncludeInTest = entity.IncludeInTest,
            IntentConfidence = entity.IntentConfidence,
            GeneratedCode = entity.GeneratedCode,
            Assertions = assertions
        };
    }

    #endregion
}
