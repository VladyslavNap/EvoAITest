namespace EvoAITest.ApiService.Models;

/// <summary>
/// DTO for visual comparison result response.
/// </summary>
public sealed class VisualComparisonDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string CheckpointName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public double DifferencePercentage { get; set; }
    public double Tolerance { get; set; }
    public int PixelsDifferent { get; set; }
    public int TotalPixels { get; set; }
    public double? SsimScore { get; set; }
    public string? DifferenceType { get; set; }
    public string BaselineUrl { get; set; } = string.Empty;
    public string ActualUrl { get; set; } = string.Empty;
    public string DiffUrl { get; set; } = string.Empty;
    public List<DifferenceRegionDto> Regions { get; set; } = new();
    public DateTimeOffset ComparedAt { get; set; }
}

/// <summary>
/// DTO for difference region.
/// </summary>
public sealed class DifferenceRegionDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double DifferenceScore { get; set; }
}

/// <summary>
/// DTO for visual baseline response.
/// </summary>
public sealed class VisualBaselineDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string CheckpointName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string Viewport { get; set; } = string.Empty;
    public string BaselineUrl { get; set; } = string.Empty;
    public string ImageHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public string? GitCommit { get; set; }
    public string? GitBranch { get; set; }
}

/// <summary>
/// Request DTO for updating checkpoint tolerance.
/// </summary>
public sealed class UpdateToleranceRequest
{
    public double NewTolerance { get; set; }
    public bool ApplyToAll { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for approving a baseline.
/// </summary>
public sealed class ApproveBaselineRequest
{
    public string Reason { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for comparison history list.
/// </summary>
public sealed class ComparisonHistoryResponse
{
    public List<VisualComparisonDto> Comparisons { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Response DTO for task checkpoints list.
/// </summary>
public sealed class TaskCheckpointsResponse
{
    public Guid TaskId { get; set; }
    public List<CheckpointSummaryDto> Checkpoints { get; set; } = new();
}

/// <summary>
/// DTO for checkpoint summary.
/// </summary>
public sealed class CheckpointSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Tolerance { get; set; }
    public bool HasBaseline { get; set; }
    public VisualComparisonDto? LatestComparison { get; set; }
}
