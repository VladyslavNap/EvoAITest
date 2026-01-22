using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using EvoAITest.Core.Models;
using EvoAITest.Core.Data.Models;
using EvoAITest.Core.Models.Execution;
using EvoAITest.Core.Models.Analytics;

namespace EvoAITest.Core.Data;

/// <summary>
/// Entity Framework Core database context for EvoAITest.
/// Manages AutomationTask and ExecutionHistory entities with Azure SQL Database.
/// </summary>
public sealed class EvoAIDbContext : DbContext
{
    /// <summary>
    /// JSON serializer options for consistent serialization/deserialization behavior.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="EvoAIDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public EvoAIDbContext(DbContextOptions<EvoAIDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the AutomationTasks DbSet.
    /// </summary>
    public DbSet<AutomationTask> AutomationTasks => Set<AutomationTask>();

    /// <summary>
    /// Gets or sets the ExecutionHistory DbSet.
    /// </summary>
    public DbSet<ExecutionHistory> ExecutionHistory => Set<ExecutionHistory>();

    /// <summary>
    /// Gets or sets the VisualBaselines DbSet.
    /// </summary>
    public DbSet<VisualBaseline> VisualBaselines => Set<VisualBaseline>();

    /// <summary>
    /// Gets or sets the VisualComparisonResults DbSet.
    /// </summary>
    public DbSet<VisualComparisonResult> VisualComparisonResults => Set<VisualComparisonResult>();

    /// <summary>
    /// Gets or sets the WaitHistory DbSet.
    /// </summary>
    public DbSet<WaitHistory> WaitHistory => Set<WaitHistory>();

    /// <summary>
    /// Gets or sets the SelectorHealingHistory DbSet.
    /// </summary>
    public DbSet<SelectorHealingHistory> SelectorHealingHistory => Set<SelectorHealingHistory>();

    /// <summary>
    /// Gets or sets the RecoveryHistory DbSet.
    /// </summary>
    public DbSet<RecoveryHistory> RecoveryHistory => Set<RecoveryHistory>();

    /// <summary>
    /// Gets or sets the RecordingSessions DbSet.
    /// </summary>
    public DbSet<RecordingSessionEntity> RecordingSessions => Set<RecordingSessionEntity>();

    /// <summary>
    /// Gets or sets the RecordedInteractions DbSet.
    /// </summary>
    public DbSet<RecordedInteractionEntity> RecordedInteractions => Set<RecordedInteractionEntity>();

    /// <summary>
    /// Gets or sets the TestExecutionResults DbSet.
    /// </summary>
    public DbSet<TestExecutionResult> TestExecutionResults => Set<TestExecutionResult>();

    /// <summary>
    /// Gets or sets the TestExecutionSessions DbSet.
    /// </summary>
    public DbSet<TestExecutionSession> TestExecutionSessions => Set<TestExecutionSession>();

    /// <summary>
    /// Gets or sets the FlakyTestAnalyses DbSet.
    /// </summary>
    public DbSet<FlakyTestAnalysis> FlakyTestAnalyses => Set<FlakyTestAnalysis>();

    /// <summary>
    /// Gets or sets the TestTrends DbSet.
    /// </summary>
    public DbSet<TestTrend> TestTrends => Set<TestTrend>();

    /// <summary>
    /// Gets or sets the AlertRules DbSet.
    /// </summary>
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();

    /// <summary>
    /// Gets or sets the AlertHistory DbSet.
    /// </summary>
    public DbSet<AlertHistory> AlertHistory => Set<AlertHistory>();

    /// <summary>
    /// Configures the database model using the specified model builder.
    /// </summary>
    /// <param name="modelBuilder">The model builder to use.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure AutomationTask entity
        modelBuilder.Entity<AutomationTask>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Table name
            entity.ToTable("AutomationTasks");

            // Required fields
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.NaturalLanguagePrompt).IsRequired();
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // JSON column for Plan
            entity.Property(e => e.Plan)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<ExecutionStep>>(v, JsonOptions) ?? new List<ExecutionStep>()
                );

            // JSON column for VisualCheckpoints
            entity.Property(e => e.VisualCheckpoints)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            // JSON column for Metadata
            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            // Configure Status enum as string
            entity.Property(e => e.Status)
                .HasColumnType("nvarchar(50)")
                .HasConversion<string>();

            // Indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.Status });

            // Relationships
            entity.HasMany(e => e.Executions)
                .WithOne(e => e.Task)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ExecutionHistory entity
        modelBuilder.Entity<ExecutionHistory>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Table name
            entity.ToTable("ExecutionHistory");

            // Required fields
            entity.Property(e => e.TaskId).IsRequired();
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StartedAt).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // JSON columns
            entity.Property(e => e.StepResults)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.ScreenshotUrls)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            // Visual regression fields
            entity.Property(e => e.VisualComparisonResults)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.VisualCheckpointsPassed).IsRequired();
            entity.Property(e => e.VisualCheckpointsFailed).IsRequired();

            entity.Property(e => e.VisualStatus)
                .HasColumnType("nvarchar(50)")
                .HasConversion<string>();

            // Configure ExecutionStatus enum as string
            entity.Property(e => e.ExecutionStatus)
                .HasColumnType("nvarchar(50)")
                .HasConversion<string>();

            // Indexes
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.ExecutionStatus);
            entity.HasIndex(e => e.StartedAt);

            // Relationship to AutomationTask
            entity.HasOne(e => e.Task)
                .WithMany(t => t.Executions)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure VisualBaseline entity
        modelBuilder.Entity<VisualBaseline>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Table name
            entity.ToTable("VisualBaselines");

            // Required fields
            entity.Property(e => e.TaskId).IsRequired();
            entity.Property(e => e.CheckpointName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Environment).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Browser).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Viewport).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BaselinePath).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImageHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ApprovedBy).IsRequired().HasMaxLength(100);

            // Optional fields
            entity.Property(e => e.GitCommit).HasMaxLength(40);
            entity.Property(e => e.GitBranch).HasMaxLength(200);
            entity.Property(e => e.BuildVersion).HasMaxLength(50);
            entity.Property(e => e.UpdateReason).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)").IsRequired();

            // Unique constraint for baseline per task/checkpoint/environment/browser/viewport
            entity.HasIndex(e => new { e.TaskId, e.CheckpointName, e.Environment, e.Browser, e.Viewport })
                .IsUnique();

            // Additional indexes
            entity.HasIndex(e => e.GitBranch);
            entity.HasIndex(e => e.CreatedAt);

            // Relationships
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Self-referencing relationship for history
            entity.HasOne<VisualBaseline>()
                .WithMany()
                .HasForeignKey(e => e.PreviousBaselineId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure VisualComparisonResult entity
        modelBuilder.Entity<VisualComparisonResult>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Table name
            entity.ToTable("VisualComparisonResults");

            // Required fields
            entity.Property(e => e.TaskId).IsRequired();
            entity.Property(e => e.ExecutionHistoryId).IsRequired();
            entity.Property(e => e.CheckpointName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BaselinePath).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ActualPath).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.DiffPath).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.DifferencePercentage).IsRequired();
            entity.Property(e => e.Tolerance).IsRequired();
            entity.Property(e => e.Passed).IsRequired();
            entity.Property(e => e.PixelsDifferent).IsRequired();
            entity.Property(e => e.TotalPixels).IsRequired();
            entity.Property(e => e.ComparedAt).IsRequired();

            // Optional fields
            entity.Property(e => e.DifferenceType).HasMaxLength(50);
            entity.Property(e => e.Regions).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)").IsRequired();

            // Indexes
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.ExecutionHistoryId);
            entity.HasIndex(e => e.Passed);
            entity.HasIndex(e => e.ComparedAt);

            // Relationships
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ExecutionHistory)
                .WithMany()
                .HasForeignKey(e => e.ExecutionHistoryId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycles

            entity.HasOne(e => e.Baseline)
                .WithMany()
                .HasForeignKey(e => e.BaselineId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycles
        });

        // Configure SelectorHealingHistory entity
        modelBuilder.Entity<SelectorHealingHistory>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Table name
            entity.ToTable("SelectorHealingHistory");

            // Required fields
            entity.Property(e => e.TaskId).IsRequired();
            entity.Property(e => e.OriginalSelector).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HealedSelector).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HealingStrategy).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ConfidenceScore).IsRequired();
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.HealedAt).IsRequired();

            // Optional fields
            entity.Property(e => e.PageUrl).HasMaxLength(2000);
            entity.Property(e => e.ExpectedText).HasMaxLength(500);
            entity.Property(e => e.Context).HasColumnType("nvarchar(max)");

            // Indexes
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.HealedAt);
            entity.HasIndex(e => e.HealingStrategy);

            // Relationships
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WaitHistory entity
        modelBuilder.Entity<WaitHistory>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Table name
            entity.ToTable("WaitHistory");

            // Required fields
            entity.Property(e => e.TaskId).IsRequired();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WaitCondition).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TimeoutMs).IsRequired();
            entity.Property(e => e.ActualWaitMs).IsRequired();
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.RecordedAt).IsRequired();

            // Optional fields
            entity.Property(e => e.Selector).HasMaxLength(500);
            entity.Property(e => e.PageUrl).HasMaxLength(2000);

            // Indexes
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.RecordedAt);

            // Relationships
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RecoveryHistory entity
        modelBuilder.Entity<RecoveryHistory>(entity =>
        {
            entity.ToTable("RecoveryHistory");
            entity.HasKey(e => e.Id);
            
            // Required string properties
            entity.Property(e => e.ErrorType)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.ErrorMessage)
                .IsRequired();
            
            entity.Property(e => e.ExceptionType)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.RecoveryStrategy)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.RecoveryActions)
                .IsRequired();
            
            // Required value properties
            entity.Property(e => e.Success)
                .IsRequired();
            
            entity.Property(e => e.AttemptNumber)
                .IsRequired();
            
            entity.Property(e => e.DurationMs)
                .IsRequired();
            
            entity.Property(e => e.RecoveredAt)
                .IsRequired();
            
            // Optional string properties
            entity.Property(e => e.PageUrl)
                .HasMaxLength(2000);
            
            entity.Property(e => e.Action)
                .HasMaxLength(200);
            
            entity.Property(e => e.Selector)
                .HasMaxLength(500);
            
            // Indexes for performance
            entity.HasIndex(e => e.TaskId)
                .HasDatabaseName("IX_RecoveryHistory_TaskId");
            
            entity.HasIndex(e => e.ErrorType)
                .HasDatabaseName("IX_RecoveryHistory_ErrorType");
            
            entity.HasIndex(e => e.Success)
                .HasDatabaseName("IX_RecoveryHistory_Success");
            
            entity.HasIndex(e => e.RecoveredAt)
                .HasDatabaseName("IX_RecoveryHistory_RecoveredAt");
            
            // Composite index for learning queries
            entity.HasIndex(e => new { e.ErrorType, e.Success })
                .HasDatabaseName("IX_RecoveryHistory_ErrorType_Success");
            
            // Relationship to AutomationTask
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RecordingSession entity
        modelBuilder.Entity<RecordingSessionEntity>(entity =>
        {
            entity.ToTable("RecordingSessions");
            entity.HasKey(e => e.Id);

            // Required string properties
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.StartUrl)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(e => e.Browser)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.TestFramework)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .IsRequired();

            // Required value properties
            entity.Property(e => e.StartedAt)
                .IsRequired();

            entity.Property(e => e.ViewportWidth)
                .IsRequired();

            entity.Property(e => e.ViewportHeight)
                .IsRequired();

            // JSON properties
            entity.Property(e => e.ConfigurationJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.MetricsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.TagsJson)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.GeneratedTestCode)
                .HasColumnType("nvarchar(max)");

            // Optional properties
            entity.Property(e => e.EndedAt);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(256);

            // Indexes for performance
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_RecordingSessions_Status");

            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_RecordingSessions_StartedAt");

            entity.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("IX_RecordingSessions_CreatedBy");

            entity.HasIndex(e => new { e.Status, e.StartedAt })
                .HasDatabaseName("IX_RecordingSessions_Status_StartedAt");

            // Relationship to interactions
            entity.HasMany(e => e.Interactions)
                .WithOne(i => i.Session)
                .HasForeignKey(i => i.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RecordedInteraction entity
        modelBuilder.Entity<RecordedInteractionEntity>(entity =>
        {
            entity.ToTable("RecordedInteractions");
            entity.HasKey(e => e.Id);

            // Required properties
            entity.Property(e => e.SessionId)
                .IsRequired();

            entity.Property(e => e.SequenceNumber)
                .IsRequired();

            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Intent)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.IntentConfidence)
                .IsRequired();

            entity.Property(e => e.IncludeInTest)
                .IsRequired();

            // JSON properties
            entity.Property(e => e.ContextJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(e => e.AssertionsJson)
                .HasColumnType("nvarchar(max)");

            // Optional string properties
            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.InputValue)
                .HasMaxLength(2000);

            entity.Property(e => e.Key)
                .HasMaxLength(50);

            entity.Property(e => e.GeneratedCode)
                .HasColumnType("nvarchar(max)");

            // Optional value properties
            entity.Property(e => e.DurationMs);
            entity.Property(e => e.CoordinateX);
            entity.Property(e => e.CoordinateY);

            // Indexes for performance
            entity.HasIndex(e => e.SessionId)
                .HasDatabaseName("IX_RecordedInteractions_SessionId");

            entity.HasIndex(e => new { e.SessionId, e.SequenceNumber })
                .HasDatabaseName("IX_RecordedInteractions_SessionId_Sequence");

            entity.HasIndex(e => e.ActionType)
                .HasDatabaseName("IX_RecordedInteractions_ActionType");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_RecordedInteractions_Timestamp");

            // Relationship to session
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Interactions)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TestExecutionResult entity
        modelBuilder.Entity<TestExecutionResult>(entity =>
        {
            entity.ToTable("TestExecutionResults");
            entity.HasKey(e => e.Id);

            // Required properties
            entity.Property(e => e.RecordingSessionId)
                .IsRequired();

            entity.Property(e => e.TestName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.TestFramework)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.StartedAt)
                .IsRequired();

            // Optional properties
            entity.Property(e => e.ErrorMessage)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.StackTrace)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.StandardOutput)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.ErrorOutput)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Environment)
                .HasMaxLength(500);

            // JSON columns
            entity.Property(e => e.StepResults)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<TestStepResult>>(v, JsonOptions) ?? new List<TestStepResult>()
                );

            entity.Property(e => e.Artifacts)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<TestArtifact>>(v, JsonOptions) ?? new List<TestArtifact>()
                );

            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new Dictionary<string, string>()
                );

            entity.Property(e => e.CompilationErrors)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>()
                );

            // Indexes for performance
            entity.HasIndex(e => e.RecordingSessionId)
                .HasDatabaseName("IX_TestExecutionResults_RecordingSessionId");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_TestExecutionResults_Status");

            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_TestExecutionResults_StartedAt");

            entity.HasIndex(e => new { e.RecordingSessionId, e.Status })
                .HasDatabaseName("IX_TestExecutionResults_RecordingSessionId_Status");

            entity.HasIndex(e => new { e.Status, e.StartedAt })
                .HasDatabaseName("IX_TestExecutionResults_Status_StartedAt");
        });

        // Configure TestExecutionSession entity
        modelBuilder.Entity<TestExecutionSession>(entity =>
        {
            entity.ToTable("TestExecutionSessions");
            entity.HasKey(e => e.Id);

            // Required properties
            entity.Property(e => e.RecordingSessionId)
                .IsRequired();

            entity.Property(e => e.TestFramework)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.StartedAt)
                .IsRequired();

            // Optional properties
            entity.Property(e => e.TestCode)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.ErrorMessage)
                .HasColumnType("nvarchar(max)");

            // JSON columns
            entity.Property(e => e.StepResults)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<TestStepResult>>(v, JsonOptions) ?? new List<TestStepResult>()
                );

            entity.Property(e => e.Artifacts)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<TestArtifact>>(v, JsonOptions) ?? new List<TestArtifact>()
                );

            entity.Property(e => e.ConsoleOutput)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>()
                );

            entity.Property(e => e.ErrorOutput)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>()
                );

            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new Dictionary<string, string>()
                );

            // Indexes for performance
            entity.HasIndex(e => e.RecordingSessionId)
                .HasDatabaseName("IX_TestExecutionSessions_RecordingSessionId");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_TestExecutionSessions_Status");

            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_TestExecutionSessions_StartedAt");
        });

        // Configure FlakyTestAnalysis entity
        modelBuilder.Entity<FlakyTestAnalysis>(entity =>
        {
            entity.ToTable("FlakyTestAnalyses");
            entity.HasKey(e => e.Id);

            // Required properties
            entity.Property(e => e.RecordingSessionId)
                .IsRequired();

            entity.Property(e => e.TestName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.FlakinessScore)
                .IsRequired();

            entity.Property(e => e.Severity)
                .HasMaxLength(50)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.AnalyzedAt)
                .IsRequired();

            // JSON columns
            entity.Property(e => e.Patterns)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<FlakyTestPattern>>(v, JsonOptions) ?? new List<FlakyTestPattern>()
                );

            entity.Property(e => e.Recommendations)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>()
                );

            entity.Property(e => e.RootCauses)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>()
                );

            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new Dictionary<string, string>()
                );

            // Indexes for analytics queries
            entity.HasIndex(e => e.RecordingSessionId)
                .HasDatabaseName("IX_FlakyTestAnalyses_RecordingSessionId");

            entity.HasIndex(e => e.Severity)
                .HasDatabaseName("IX_FlakyTestAnalyses_Severity");

            entity.HasIndex(e => e.FlakinessScore)
                .HasDatabaseName("IX_FlakyTestAnalyses_FlakinessScore");

            entity.HasIndex(e => e.AnalyzedAt)
                .HasDatabaseName("IX_FlakyTestAnalyses_AnalyzedAt");

            entity.HasIndex(e => new { e.RecordingSessionId, e.FlakinessScore })
                .HasDatabaseName("IX_FlakyTestAnalyses_RecordingId_Score");
        });

        // Configure TestTrend entity
        modelBuilder.Entity<TestTrend>(entity =>
        {
            entity.ToTable("TestTrends");
            entity.HasKey(e => e.Id);

            // Required properties
            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.Interval)
                .HasMaxLength(50)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.CalculatedAt)
                .IsRequired();

            // Optional properties
            entity.Property(e => e.TestName)
                .HasMaxLength(500);

            // JSON column for additional metrics
            entity.Property(e => e.Metrics)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, JsonOptions) ?? new Dictionary<string, double>()
                );

            // Indexes for trend queries
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_TestTrends_Timestamp");

            entity.HasIndex(e => e.RecordingSessionId)
                .HasDatabaseName("IX_TestTrends_RecordingSessionId");

            entity.HasIndex(e => e.Interval)
                .HasDatabaseName("IX_TestTrends_Interval");

            entity.HasIndex(e => new { e.RecordingSessionId, e.Timestamp })
                .HasDatabaseName("IX_TestTrends_RecordingId_Timestamp");

            entity.HasIndex(e => new { e.Interval, e.Timestamp })
                .HasDatabaseName("IX_TestTrends_Interval_Timestamp");

                    // Composite index for most common query pattern
                    entity.HasIndex(e => new { e.RecordingSessionId, e.Interval, e.Timestamp })
                        .HasDatabaseName("IX_TestTrends_RecordingId_Interval_Timestamp");
                });

                // Configure AlertRule entity
                modelBuilder.Entity<AlertRule>(entity =>
                {
                    entity.ToTable("AlertRules");
                    entity.HasKey(e => e.Id);

                    // Required fields
                    entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                    entity.Property(e => e.Description).HasMaxLength(1000);
                    entity.Property(e => e.Metric).IsRequired().HasMaxLength(100);

                    // Enum conversions
                    entity.Property(e => e.Operator)
                        .HasColumnType("nvarchar(50)")
                        .HasConversion<string>()
                        .IsRequired();

                    entity.Property(e => e.Severity)
                        .HasColumnType("nvarchar(50)")
                        .HasConversion<string>()
                        .IsRequired();

                    // JSON columns
                    entity.Property(e => e.Channels)
                        .HasColumnType("nvarchar(max)")
                        .IsRequired();

                    entity.Property(e => e.Recipients)
                        .HasColumnType("nvarchar(max)")
                        .IsRequired();

                    entity.Property(e => e.Metadata)
                        .HasColumnType("nvarchar(max)")
                        .HasConversion(
                            v => JsonSerializer.Serialize(v, JsonOptions),
                            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new Dictionary<string, string>()
                        );

                    // Indexes
                    entity.HasIndex(e => e.Enabled)
                        .HasFilter("[Enabled] = 1")
                        .HasDatabaseName("IX_AlertRules_Enabled");

                    entity.HasIndex(e => e.Metric)
                        .HasDatabaseName("IX_AlertRules_Metric");

                    entity.HasIndex(e => e.RecordingSessionId)
                        .HasDatabaseName("IX_AlertRules_RecordingSessionId");

                    entity.HasIndex(e => e.Severity)
                        .HasDatabaseName("IX_AlertRules_Severity");

                    entity.HasIndex(e => e.LastTriggeredAt)
                        .HasDatabaseName("IX_AlertRules_LastTriggered");

                    entity.HasIndex(e => new { e.Enabled, e.Metric })
                        .HasFilter("[Enabled] = 1")
                        .HasDatabaseName("IX_AlertRules_Enabled_Metric");

                    // Relationships
                    entity.HasMany<AlertHistory>()
                        .WithOne(h => h.AlertRule)
                        .HasForeignKey(h => h.AlertRuleId)
                        .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure AlertHistory entity
                modelBuilder.Entity<AlertHistory>(entity =>
                {
                    entity.ToTable("AlertHistory");
                    entity.HasKey(e => e.Id);

                    // Required fields
                    entity.Property(e => e.AlertRuleId).IsRequired();
                    entity.Property(e => e.TriggeredAt).IsRequired();
                    entity.Property(e => e.Message).IsRequired();
                    entity.Property(e => e.ChannelsNotified).IsRequired();

                    // Enum conversion
                    entity.Property(e => e.Severity)
                        .HasColumnType("nvarchar(50)")
                        .HasConversion<string>()
                        .IsRequired();

                    // JSON columns
                    entity.Property(e => e.Context)
                        .HasColumnType("nvarchar(max)")
                        .HasConversion(
                            v => JsonSerializer.Serialize(v, JsonOptions),
                            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new Dictionary<string, string>()
                        );

                    // Indexes
                    entity.HasIndex(e => e.AlertRuleId)
                        .HasDatabaseName("IX_AlertHistory_AlertRuleId");

                    entity.HasIndex(e => e.TriggeredAt)
                        .HasDatabaseName("IX_AlertHistory_TriggeredAt");

                    entity.HasIndex(e => e.Acknowledged)
                        .HasFilter("[Acknowledged] = 0")
                        .HasDatabaseName("IX_AlertHistory_Acknowledged");

                    entity.HasIndex(e => e.RecordingSessionId)
                        .HasDatabaseName("IX_AlertHistory_RecordingSessionId");

                    entity.HasIndex(e => e.Severity)
                        .HasDatabaseName("IX_AlertHistory_Severity");

                    entity.HasIndex(e => new { e.AlertRuleId, e.TriggeredAt })
                        .HasDatabaseName("IX_AlertHistory_RuleId_Triggered");

                    // Relationships
                    entity.HasOne(e => e.AlertRule)
                        .WithMany()
                        .HasForeignKey(e => e.AlertRuleId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
            }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// Automatically updates UpdatedAt timestamps for AutomationTask entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically update UpdatedAt timestamp for modified AutomationTask entities
        var entries = ChangeTracker.Entries<AutomationTask>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all changes made in this context to the database (synchronously).
    /// Automatically updates UpdatedAt timestamps for AutomationTask entities.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        // Automatically update UpdatedAt timestamp for modified AutomationTask entities
        var entries = ChangeTracker.Entries<AutomationTask>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return base.SaveChanges();
    }
}
