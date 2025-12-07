using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EvoAITest.Core.Models;

/// <summary>
/// Represents a visual checkpoint for regression testing.
/// Defines a point in automation where a screenshot should be captured and compared.
/// </summary>
public sealed class VisualCheckpoint
{
    /// <summary>
    /// Gets or sets the unique identifier for this checkpoint.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the execution step this checkpoint is associated with.
    /// </summary>
    public int StepNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the human-readable name for this checkpoint.
    /// </summary>
    /// <example>LoginPage_AfterLoad, Dashboard_AfterFilter</example>
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets or sets the type of checkpoint.
    /// </summary>
    public CheckpointType Type { get; init; } = CheckpointType.FullPage;
    
    /// <summary>
    /// Gets or sets the CSS selector for element-based screenshots (optional).
    /// </summary>
    [MaxLength(500)]
    public string? Selector { get; init; }
    
    /// <summary>
    /// Gets or sets the region coordinates for partial screenshots (optional).
    /// </summary>
    public ScreenshotRegion? Region { get; init; }
    
    /// <summary>
    /// Gets or sets the comparison tolerance (0.0 = exact match, 1.0 = ignore all differences).
    /// </summary>
    /// <remarks>Default is 0.01 (1% difference allowed).</remarks>
    public double Tolerance { get; init; } = 0.01;
    
    /// <summary>
    /// Gets or sets CSS selectors for elements to ignore during comparison.
    /// </summary>
    /// <remarks>
    /// Use this to exclude dynamic content like timestamps, ads, or real-time data.
    /// </remarks>
    public List<string> IgnoreSelectors { get; init; } = new();
    
    /// <summary>
    /// Gets or sets whether this checkpoint is required (fails test) or optional (warning only).
    /// </summary>
    public bool IsRequired { get; init; } = true;
    
    /// <summary>
    /// Gets or sets tags for categorizing checkpoints.
    /// </summary>
    /// <example>critical, responsive, dark-mode</example>
    public List<string> Tags { get; init; } = new();
    
    /// <summary>
    /// Gets or sets additional metadata for this checkpoint.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Defines the type of visual checkpoint.
/// </summary>
public enum CheckpointType
{
    /// <summary>Full page screenshot including scrollable content.</summary>
    FullPage,
    
    /// <summary>Single element screenshot.</summary>
    Element,
    
    /// <summary>Specific rectangular region.</summary>
    Region,
    
    /// <summary>Current viewport only.</summary>
    Viewport
}

/// <summary>
/// Represents a rectangular region for partial screenshots.
/// </summary>
public sealed class ScreenshotRegion
{
    /// <summary>Gets or sets the X coordinate of the top-left corner.</summary>
    public int X { get; init; }
    
    /// <summary>Gets or sets the Y coordinate of the top-left corner.</summary>
    public int Y { get; init; }
    
    /// <summary>Gets or sets the width of the region.</summary>
    public int Width { get; init; }
    
    /// <summary>Gets or sets the height of the region.</summary>
    public int Height { get; init; }
}

/// <summary>
/// Database entity for storing visual baselines.
/// </summary>
[Table("VisualBaselines")]
[Index(nameof(TaskId), nameof(CheckpointName), nameof(Environment), nameof(Browser), IsUnique = true)]
public sealed class VisualBaseline
{
    /// <summary>
    /// Gets or sets the unique identifier for this baseline.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the task ID this baseline belongs to.
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }
    
    /// <summary>
    /// Gets or sets the checkpoint name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CheckpointName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the environment (dev, staging, prod).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Environment { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the browser (chromium, firefox, webkit).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Browser { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the viewport size (e.g., "1920x1080").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Viewport { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the file path or blob storage URL for the baseline image.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string BaselinePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the SHA256 hash of the baseline image for integrity checking.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ImageHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when this baseline was created.
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Gets or sets who approved this baseline (user ID or "auto").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ApprovedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the Git commit hash when baseline was created.
    /// </summary>
    [MaxLength(40)]
    public string? GitCommit { get; set; }
    
    /// <summary>
    /// Gets or sets the Git branch name.
    /// </summary>
    [MaxLength(200)]
    public string? GitBranch { get; set; }
    
    /// <summary>
    /// Gets or sets the build number or version.
    /// </summary>
    [MaxLength(50)]
    public string? BuildVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the previous baseline ID (for tracking history).
    /// </summary>
    public Guid? PreviousBaselineId { get; set; }
    
    /// <summary>
    /// Gets or sets the reason for baseline update.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? UpdateReason { get; set; }
    
    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string Metadata { get; set; } = "{}";
    
    // Navigation properties
    [ForeignKey(nameof(TaskId))]
    public AutomationTask? Task { get; set; }
}

/// <summary>
/// Database entity for storing visual comparison results.
/// </summary>
[Table("VisualComparisonResults")]
[Index(nameof(TaskId))]
[Index(nameof(ExecutionHistoryId))]
[Index(nameof(Passed))]
public sealed class VisualComparisonResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this comparison result.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the task ID.
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }
    
    /// <summary>
    /// Gets or sets the execution history ID.
    /// </summary>
    [Required]
    public Guid ExecutionHistoryId { get; set; }
    
    /// <summary>
    /// Gets or sets the checkpoint name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CheckpointName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the baseline ID used for comparison.
    /// </summary>
    public Guid? BaselineId { get; set; }
    
    /// <summary>
    /// Gets or sets the baseline image path.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string BaselinePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the actual screenshot path.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ActualPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the diff image path.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string DiffPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the difference percentage (0.0 to 1.0).
    /// </summary>
    [Required]
    public double DifferencePercentage { get; set; }
    
    /// <summary>
    /// Gets or sets the tolerance used for comparison.
    /// </summary>
    [Required]
    public double Tolerance { get; set; }
    
    /// <summary>
    /// Gets or sets whether the comparison passed.
    /// </summary>
    [Required]
    public bool Passed { get; set; }
    
    /// <summary>
    /// Gets or sets the number of pixels that differ.
    /// </summary>
    [Required]
    public int PixelsDifferent { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of pixels.
    /// </summary>
    [Required]
    public int TotalPixels { get; set; }
    
    /// <summary>
    /// Gets or sets the SSIM (Structural Similarity Index) score.
    /// </summary>
    public double? SsimScore { get; set; }
    
    /// <summary>
    /// Gets or sets the type of difference detected.
    /// </summary>
    [MaxLength(50)]
    public string? DifferenceType { get; set; }
    
    /// <summary>
    /// Gets or sets the JSON-serialized difference regions.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string Regions { get; set; } = "[]";
    
    /// <summary>
    /// Gets or sets when this comparison was performed.
    /// </summary>
    [Required]
    public DateTimeOffset ComparedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string Metadata { get; set; } = "{}";
    
    // Navigation properties
    [ForeignKey(nameof(TaskId))]
    public AutomationTask? Task { get; set; }
    
    [ForeignKey(nameof(ExecutionHistoryId))]
    public ExecutionHistory? ExecutionHistory { get; set; }
    
    [ForeignKey(nameof(BaselineId))]
    public VisualBaseline? Baseline { get; set; }
}

/// <summary>
/// Represents a region where differences were detected.
/// </summary>
public sealed class DifferenceRegion
{
    /// <summary>Gets or sets the X coordinate of the region.</summary>
    public int X { get; init; }
    
    /// <summary>Gets or sets the Y coordinate of the region.</summary>
    public int Y { get; init; }
    
    /// <summary>Gets or sets the width of the region.</summary>
    public int Width { get; init; }
    
    /// <summary>Gets or sets the height of the region.</summary>
    public int Height { get; init; }
    
    /// <summary>Gets or sets the difference score for this region (0.0 to 1.0).</summary>
    public double DifferenceScore { get; init; }
}

/// <summary>
/// Defines the overall visual regression status for an execution.
/// </summary>
public enum VisualRegressionStatus
{
    /// <summary>No visual checks were performed in this execution.</summary>
    NotApplicable,
    
    /// <summary>All visual checks passed within tolerance.</summary>
    Passed,
    
    /// <summary>One or more required visual checks failed.</summary>
    Failed,
    
    /// <summary>Optional visual checks failed but all required checks passed.</summary>
    Warning
}

/// <summary>
/// Defines types of visual differences detected.
/// </summary>
public enum DifferenceType
{
    /// <summary>No difference detected.</summary>
    NoDifference,
    
    /// <summary>Minor rendering differences (anti-aliasing, font rendering).</summary>
    MinorRendering,
    
    /// <summary>Actual content changed.</summary>
    ContentChange,
    
    /// <summary>Elements moved or layout shifted.</summary>
    LayoutShift,
    
    /// <summary>Color or theme changed.</summary>
    ColorChange,
    
    /// <summary>Element sizing changed.</summary>
    SizeChange
}
