using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EvoAITest.Core.Models.Accessibility;

namespace EvoAITest.Core.Abstractions;

public interface IAccessibilityService
{
    Task<AccessibilityReport> SaveReportAsync(AccessibilityReport report, CancellationToken cancellationToken = default);
    Task<AccessibilityReport?> GetReportAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AccessibilityReport>> GetReportsByTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<List<AccessibilityReport>> GetReportsResultsAsync(Guid executionHistoryId, CancellationToken cancellationToken = default);
    Task DeleteReportAsync(Guid id, CancellationToken cancellationToken = default);
}
