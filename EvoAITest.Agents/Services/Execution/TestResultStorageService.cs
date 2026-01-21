using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Agents.Services.Execution;

/// <summary>
/// Service for persisting and retrieving test execution results from database
/// </summary>
public sealed class TestResultStorageService : ITestResultStorage
{
    private readonly ILogger<TestResultStorageService> _logger;
    private readonly EvoAIDbContext _dbContext;

    public TestResultStorageService(
        ILogger<TestResultStorageService> logger,
        EvoAIDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<TestExecutionResult> SaveResultAsync(
        TestExecutionResult result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Saving execution result {ResultId} for recording {RecordingId}",
            result.Id,
            result.RecordingSessionId);

        _dbContext.TestExecutionResults.Add(result);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Execution result {ResultId} saved successfully", result.Id);

        return result;
    }

    public async Task<TestExecutionResult?> GetResultAsync(
        Guid resultId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving execution result {ResultId}", resultId);

        return await _dbContext.TestExecutionResults
            .FirstOrDefaultAsync(r => r.Id == resultId, cancellationToken);
    }

    public async Task<IEnumerable<TestExecutionResult>> GetResultsByRecordingAsync(
        Guid recordingSessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Retrieving execution results for recording {RecordingId}",
            recordingSessionId);

        return await _dbContext.TestExecutionResults
            .Where(r => r.RecordingSessionId == recordingSessionId)
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TestExecutionResult>> GetExecutionHistoryAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving execution history: skip={Skip}, take={Take}", skip, take);

        return await _dbContext.TestExecutionResults
            .OrderByDescending(r => r.StartedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<TestExecutionStatistics> GetStatisticsAsync(
        Guid recordingSessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Calculating statistics for recording {RecordingId}",
            recordingSessionId);

        var results = await _dbContext.TestExecutionResults
            .Where(r => r.RecordingSessionId == recordingSessionId)
            .ToListAsync(cancellationToken);

        if (!results.Any())
        {
            return new TestExecutionStatistics
            {
                RecordingSessionId = recordingSessionId
            };
        }

        var passedResults = results.Where(r => r.Status == TestExecutionStatus.Passed).ToList();
        var failedResults = results.Where(r => r.Status == TestExecutionStatus.Failed).ToList();

        return new TestExecutionStatistics
        {
            RecordingSessionId = recordingSessionId,
            TotalExecutions = results.Count,
            PassedExecutions = passedResults.Count,
            FailedExecutions = failedResults.Count,
            AverageDurationMs = results.Any() ? (long)results.Average(r => r.DurationMs) : 0,
            MinDurationMs = results.Any() ? results.Min(r => r.DurationMs) : 0,
            MaxDurationMs = results.Any() ? results.Max(r => r.DurationMs) : 0,
            FirstExecutionAt = results.Min(r => r.StartedAt),
            LastExecutionAt = results.Max(r => r.StartedAt),
            FlakyCount = 0 // TODO: Implement flaky test detection
        };
    }

    public async Task<int> DeleteOldResultsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting execution results older than {Date}", olderThan);

        var oldResults = await _dbContext.TestExecutionResults
            .Where(r => r.StartedAt < olderThan)
            .ToListAsync(cancellationToken);

        var count = oldResults.Count;

        _dbContext.TestExecutionResults.RemoveRange(oldResults);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} old execution results", count);

        return count;
    }

    public async Task<IEnumerable<TestExecutionResult>> SearchResultsAsync(
        TestExecutionSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching execution results with criteria");

        var query = _dbContext.TestExecutionResults.AsQueryable();

        // Apply filters
        if (criteria.RecordingSessionId.HasValue)
        {
            query = query.Where(r => r.RecordingSessionId == criteria.RecordingSessionId.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(r => r.Status == criteria.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.TestFramework))
        {
            query = query.Where(r => r.TestFramework == criteria.TestFramework);
        }

        if (!string.IsNullOrWhiteSpace(criteria.TestNameContains))
        {
            query = query.Where(r => r.TestName.Contains(criteria.TestNameContains));
        }

        if (criteria.StartedAfter.HasValue)
        {
            query = query.Where(r => r.StartedAt >= criteria.StartedAfter.Value);
        }

        if (criteria.StartedBefore.HasValue)
        {
            query = query.Where(r => r.StartedAt <= criteria.StartedBefore.Value);
        }

        if (criteria.MinDurationMs.HasValue)
        {
            query = query.Where(r => r.DurationMs >= criteria.MinDurationMs.Value);
        }

        if (criteria.MaxDurationMs.HasValue)
        {
            query = query.Where(r => r.DurationMs <= criteria.MaxDurationMs.Value);
        }

        // Apply sorting
        query = criteria.SortBy.ToLowerInvariant() switch
        {
            "testname" => criteria.SortDescending 
                ? query.OrderByDescending(r => r.TestName) 
                : query.OrderBy(r => r.TestName),
            "status" => criteria.SortDescending 
                ? query.OrderByDescending(r => r.Status) 
                : query.OrderBy(r => r.Status),
            "duration" => criteria.SortDescending 
                ? query.OrderByDescending(r => r.DurationMs) 
                : query.OrderBy(r => r.DurationMs),
            _ => criteria.SortDescending 
                ? query.OrderByDescending(r => r.StartedAt) 
                : query.OrderBy(r => r.StartedAt)
        };

        // Apply pagination
        query = query.Skip(criteria.Skip).Take(criteria.Take);

        var results = await query.ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} execution results matching criteria", results.Count);

        return results;
    }

    public async Task UpdateStatusAsync(
        Guid resultId,
        TestExecutionStatus status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Updating status for execution result {ResultId} to {Status}",
            resultId,
            status);

        var result = await _dbContext.TestExecutionResults
            .FirstOrDefaultAsync(r => r.Id == resultId, cancellationToken);

        if (result == null)
        {
            throw new InvalidOperationException($"Execution result {resultId} not found");
        }

        result.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Status updated successfully for result {ResultId}", resultId);
    }
}
