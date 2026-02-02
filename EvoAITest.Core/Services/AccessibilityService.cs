using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models.Accessibility;

namespace EvoAITest.Core.Services;

public class AccessibilityService : IAccessibilityService
{
    private readonly IDbContextFactory<EvoAIDbContext> _contextFactory;

    public AccessibilityService(IDbContextFactory<EvoAIDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<AccessibilityReport> SaveReportAsync(AccessibilityReport report, CancellationToken cancellationToken = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.AccessibilityReports.Add(report);
        await context.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<AccessibilityReport?> GetReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AccessibilityReports
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
    
    public async Task<List<AccessibilityReport>> GetReportsByTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AccessibilityReports
            .Where(r => r.AutomationTaskId == taskId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AccessibilityReport>> GetReportsResultsAsync(Guid executionHistoryId, CancellationToken cancellationToken = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AccessibilityReports
            .Where(r => r.ExecutionHistoryId == executionHistoryId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var report = await context.AccessibilityReports.FindAsync(new object[] { id }, cancellationToken);
        if (report != null)
        {
            context.AccessibilityReports.Remove(report);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
