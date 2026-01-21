namespace EvoAITest.Core.Models.Execution;

/// <summary>
/// Represents an artifact generated during test execution
/// </summary>
public sealed class TestArtifact
{
    /// <summary>
    /// Unique identifier for this artifact
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Type of artifact (screenshot, log, trace, video, etc.)
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// File path or storage location
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// MIME type of the artifact
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// When the artifact was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Associated test step number, if applicable
    /// </summary>
    public int? StepNumber { get; set; }

    /// <summary>
    /// Description of the artifact
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
