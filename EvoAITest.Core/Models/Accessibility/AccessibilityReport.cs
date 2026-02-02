using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EvoAITest.Core.Models.Accessibility;

public class AccessibilityReport
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? AutomationTaskId { get; set; }
    
    public Guid? ExecutionHistoryId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int ViolationCount { get; set; }
    
    public int CriticalCount { get; set; }
    public int SeriousCount { get; set; }
    public int ModerateCount { get; set; }
    public int MinorCount { get; set; }

    public double Score { get; set; } // 0-100

    // Stored as JSON in database if using simple storage, or navigation property
    public List<AccessibilityViolation> Violations { get; set; } = new();
    
    public string ScreenshotPath { get; set; } = string.Empty;

    public Dictionary<string, object> Metadata { get; set; } = new();
}
