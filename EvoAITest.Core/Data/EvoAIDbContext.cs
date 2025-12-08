using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using EvoAITest.Core.Models;

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
