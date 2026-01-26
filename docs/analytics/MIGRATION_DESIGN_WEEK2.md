# Week 2 Analytics - Migration Design Specification

**Date:** January 2026  
**Branch:** AnalyticsDashboard  
**Target:** .NET 10 + EF Core 10  
**Status:** Design Phase - Ready for Implementation

---

## Overview

This document specifies the database schema changes required to complete Week 2: Analytics Dashboard + Historical Tracking. The design adds alert management, retention policies, and audit trails while optimizing existing analytics tables.

### Migration Strategy

**Approach:** Two separate migrations for controlled deployment
1. **Migration #1:** `AddAnalyticsAlerting` - Alert system tables (Phase 3)
2. **Migration #2:** `AddRetentionPolicies` - Retention fields + audit (Phase 4)

**Why Separate?**
- Phases 1-2 (core historical tracking) require **NO new migrations**
- Phase 3 (alerts) is independent and can be deployed separately
- Phase 4 (retention) can be deferred if needed
- Easier rollback strategy

---

## Migration #1: AddAnalyticsAlerting

### Purpose
Enable proactive monitoring via configurable alerts for analytics metrics.

### New Tables

#### 1. **AlertRules** Table

Stores alert configurations with conditions and thresholds.

```csharp
public sealed class AlertRule
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// User-friendly name for the alert
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Metric being monitored (passRate, flakinessScore, avgDuration, etc.)
    /// </summary>
    public required string Metric { get; set; }

    /// <summary>
    /// Comparison operator (LessThan, GreaterThan, Equals, etc.)
    /// </summary>
    public AlertOperator Operator { get; set; }

    /// <summary>
    /// Threshold value for comparison
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Alert severity level
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Whether this alert is currently active
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional recording session ID to scope the alert
    /// </summary>
    public Guid? RecordingSessionId { get; set; }

    /// <summary>
    /// Minimum time between alerts (throttling)
    /// </summary>
    public int ThrottleMinutes { get; set; } = 60;

    /// <summary>
    /// Notification channels (JSON array: ["email", "slack", "webhook"])
    /// </summary>
    public required string Channels { get; set; }

    /// <summary>
    /// Alert recipients (JSON object with channel-specific data)
    /// Example: { "email": ["admin@example.com"], "slack": "#alerts" }
    /// </summary>
    public required string Recipients { get; set; }

    /// <summary>
    /// When this alert was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this alert was last modified
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who created this alert
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When this alert was last triggered
    /// </summary>
    public DateTimeOffset? LastTriggeredAt { get; set; }

    /// <summary>
    /// Number of times this alert has been triggered
    /// </summary>
    public int TriggerCount { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public enum AlertOperator
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Equals,
    NotEquals,
    Between,       // Requires two threshold values
    Outside        // Requires two threshold values
}

public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
```

**SQL Schema:**
```sql
CREATE TABLE [AlertRules] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [Metric] NVARCHAR(100) NOT NULL,
    [Operator] NVARCHAR(50) NOT NULL,
    [Threshold] FLOAT NOT NULL,
    [Severity] NVARCHAR(50) NOT NULL,
    [Enabled] BIT NOT NULL DEFAULT 1,
    [RecordingSessionId] UNIQUEIDENTIFIER NULL,
    [ThrottleMinutes] INT NOT NULL DEFAULT 60,
    [Channels] NVARCHAR(MAX) NOT NULL,
    [Recipients] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIMEOFFSET NOT NULL,
    [UpdatedAt] DATETIMEOFFSET NOT NULL,
    [CreatedBy] NVARCHAR(256) NULL,
    [LastTriggeredAt] DATETIMEOFFSET NULL,
    [TriggerCount] INT NOT NULL DEFAULT 0,
    [Metadata] NVARCHAR(MAX) NOT NULL,
    
    CONSTRAINT [CK_AlertRules_Threshold] CHECK ([Threshold] >= 0),
    CONSTRAINT [CK_AlertRules_Throttle] CHECK ([ThrottleMinutes] >= 0)
);

CREATE INDEX [IX_AlertRules_Enabled] ON [AlertRules]([Enabled]) WHERE [Enabled] = 1;
CREATE INDEX [IX_AlertRules_Metric] ON [AlertRules]([Metric]);
CREATE INDEX [IX_AlertRules_RecordingSessionId] ON [AlertRules]([RecordingSessionId]);
CREATE INDEX [IX_AlertRules_Severity] ON [AlertRules]([Severity]);
CREATE INDEX [IX_AlertRules_LastTriggered] ON [AlertRules]([LastTriggeredAt]);
CREATE INDEX [IX_AlertRules_Enabled_Metric] ON [AlertRules]([Enabled], [Metric]) WHERE [Enabled] = 1;
```

**Indexes Rationale:**
- `IX_AlertRules_Enabled` - Fast lookup of active alerts (filtered index)
- `IX_AlertRules_Metric` - Group alerts by metric type
- `IX_AlertRules_RecordingSessionId` - Scope alerts to specific recordings
- `IX_AlertRules_Severity` - Filter by severity for UI
- `IX_AlertRules_LastTriggered` - Check throttling logic
- `IX_AlertRules_Enabled_Metric` - Composite for alert evaluation queries

---

#### 2. **AlertHistory** Table

Stores alert trigger events and acknowledgments.

```csharp
public sealed class AlertHistory
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Associated alert rule ID
    /// </summary>
    public Guid AlertRuleId { get; set; }

    /// <summary>
    /// Alert rule navigation property
    /// </summary>
    public AlertRule? AlertRule { get; set; }

    /// <summary>
    /// When this alert was triggered
    /// </summary>
    public DateTimeOffset TriggeredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current value of the monitored metric
    /// </summary>
    public double ActualValue { get; set; }

    /// <summary>
    /// Expected threshold value
    /// </summary>
    public double ThresholdValue { get; set; }

    /// <summary>
    /// Alert severity at time of trigger
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Alert message sent
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Channels notified (JSON array: ["email", "slack"])
    /// </summary>
    public required string ChannelsNotified { get; set; }

    /// <summary>
    /// Whether this alert has been acknowledged
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// When this alert was acknowledged
    /// </summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }

    /// <summary>
    /// User who acknowledged this alert
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Notes added during acknowledgment
    /// </summary>
    public string? AcknowledgmentNotes { get; set; }

    /// <summary>
    /// Optional recording session ID if alert is scoped
    /// </summary>
    public Guid? RecordingSessionId { get; set; }

    /// <summary>
    /// Additional context data (JSON)
    /// </summary>
    public Dictionary<string, string> Context { get; set; } = [];

    /// <summary>
    /// Whether notification was successfully delivered
    /// </summary>
    public bool NotificationSuccess { get; set; }

    /// <summary>
    /// Error message if notification failed
    /// </summary>
    public string? NotificationError { get; set; }
}
```

**SQL Schema:**
```sql
CREATE TABLE [AlertHistory] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [AlertRuleId] UNIQUEIDENTIFIER NOT NULL,
    [TriggeredAt] DATETIMEOFFSET NOT NULL,
    [ActualValue] FLOAT NOT NULL,
    [ThresholdValue] FLOAT NOT NULL,
    [Severity] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [ChannelsNotified] NVARCHAR(MAX) NOT NULL,
    [Acknowledged] BIT NOT NULL DEFAULT 0,
    [AcknowledgedAt] DATETIMEOFFSET NULL,
    [AcknowledgedBy] NVARCHAR(256) NULL,
    [AcknowledgmentNotes] NVARCHAR(MAX) NULL,
    [RecordingSessionId] UNIQUEIDENTIFIER NULL,
    [Context] NVARCHAR(MAX) NOT NULL,
    [NotificationSuccess] BIT NOT NULL DEFAULT 0,
    [NotificationError] NVARCHAR(MAX) NULL,
    
    CONSTRAINT [FK_AlertHistory_AlertRules] FOREIGN KEY ([AlertRuleId])
        REFERENCES [AlertRules]([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AlertHistory_AlertRuleId] ON [AlertHistory]([AlertRuleId]);
CREATE INDEX [IX_AlertHistory_TriggeredAt] ON [AlertHistory]([TriggeredAt]);
CREATE INDEX [IX_AlertHistory_Acknowledged] ON [AlertHistory]([Acknowledged]) WHERE [Acknowledged] = 0;
CREATE INDEX [IX_AlertHistory_RecordingSessionId] ON [AlertHistory]([RecordingSessionId]);
CREATE INDEX [IX_AlertHistory_Severity] ON [AlertHistory]([Severity]);
CREATE INDEX [IX_AlertHistory_RuleId_Triggered] ON [AlertHistory]([AlertRuleId], [TriggeredAt]);
```

**Indexes Rationale:**
- `IX_AlertHistory_AlertRuleId` - Query alert history by rule
- `IX_AlertHistory_TriggeredAt` - Time-based filtering/sorting
- `IX_AlertHistory_Acknowledged` - Show unacknowledged alerts (filtered index)
- `IX_AlertHistory_RecordingSessionId` - Scope alerts to recordings
- `IX_AlertHistory_Severity` - Filter by criticality
- `IX_AlertHistory_RuleId_Triggered` - Composite for throttling checks

---

### EF Core Configuration

```csharp
// In EvoAIDbContext.OnModelCreating()

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
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) 
                ?? new Dictionary<string, string>()
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
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) 
                ?? new Dictionary<string, string>()
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
```

---

### Migration #1 Code

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddAnalyticsAlerting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create AlertRules table
        migrationBuilder.CreateTable(
            name: "AlertRules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Metric = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Operator = table.Column<string>(type: "nvarchar(50)", nullable: false),
                Threshold = table.Column<double>(type: "float", nullable: false),
                Severity = table.Column<string>(type: "nvarchar(50)", nullable: false),
                Enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                RecordingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ThrottleMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                Channels = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Recipients = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                LastTriggeredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                TriggerCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AlertRules", x => x.Id);
                table.CheckConstraint("CK_AlertRules_Threshold", "[Threshold] >= 0");
                table.CheckConstraint("CK_AlertRules_Throttle", "[ThrottleMinutes] >= 0");
            });

        // Create AlertHistory table
        migrationBuilder.CreateTable(
            name: "AlertHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AlertRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TriggeredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ActualValue = table.Column<double>(type: "float", nullable: false),
                ThresholdValue = table.Column<double>(type: "float", nullable: false),
                Severity = table.Column<string>(type: "nvarchar(50)", nullable: false),
                Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ChannelsNotified = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Acknowledged = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                AcknowledgedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                AcknowledgmentNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RecordingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Context = table.Column<string>(type: "nvarchar(max)", nullable: false),
                NotificationSuccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                NotificationError = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AlertHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_AlertHistory_AlertRules",
                    column: x => x.AlertRuleId,
                    principalTable: "AlertRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes for AlertRules
        migrationBuilder.CreateIndex(
            name: "IX_AlertRules_Enabled",
            table: "AlertRules",
            column: "Enabled",
            filter: "[Enabled] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_AlertRules_Metric",
            table: "AlertRules",
            column: "Metric");

        migrationBuilder.CreateIndex(
            name: "IX_AlertRules_RecordingSessionId",
            table: "AlertRules",
            column: "RecordingSessionId");

        migrationBuilder.CreateIndex(
            name: "IX_AlertRules_Severity",
            table: "AlertRules",
            column: "Severity");

        migrationBuilder.CreateIndex(
            name: "IX_AlertRules_LastTriggered",
            table: "AlertRules",
            column: "LastTriggeredAt");

        migrationBuilder.CreateIndex(
            name: "IX_AlertRules_Enabled_Metric",
            table: "AlertRules",
            columns: new[] { "Enabled", "Metric" },
            filter: "[Enabled] = 1");

        // Create indexes for AlertHistory
        migrationBuilder.CreateIndex(
            name: "IX_AlertHistory_AlertRuleId",
            table: "AlertHistory",
            column: "AlertRuleId");

        migrationBuilder.CreateIndex(
            name: "IX_AlertHistory_TriggeredAt",
            table: "AlertHistory",
            column: "TriggeredAt");

        migrationBuilder.CreateIndex(
            name: "IX_AlertHistory_Acknowledged",
            table: "AlertHistory",
            column: "Acknowledged",
            filter: "[Acknowledged] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_AlertHistory_RecordingSessionId",
            table: "AlertHistory",
            column: "RecordingSessionId");

        migrationBuilder.CreateIndex(
            name: "IX_AlertHistory_Severity",
            table: "AlertHistory",
            column: "Severity");

        migrationBuilder.CreateIndex(
            name: "IX_AlertHistory_RuleId_Triggered",
            table: "AlertHistory",
            columns: new[] { "AlertRuleId", "TriggeredAt" });

        // Seed default alert rules
        migrationBuilder.InsertData(
            table: "AlertRules",
            columns: new[] { "Id", "Name", "Description", "Metric", "Operator", "Threshold", 
                           "Severity", "Enabled", "ThrottleMinutes", "Channels", "Recipients", 
                           "CreatedAt", "UpdatedAt", "TriggerCount", "Metadata" },
            values: new object[]
            {
                Guid.NewGuid(),
                "Low Pass Rate Warning",
                "Alert when overall pass rate drops below 80%",
                "passRate",
                "LessThan",
                80.0,
                "Warning",
                true,
                60,
                "[\"signalr\"]",
                "{}",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                0,
                "{}"
            });

        migrationBuilder.InsertData(
            table: "AlertRules",
            columns: new[] { "Id", "Name", "Description", "Metric", "Operator", "Threshold", 
                           "Severity", "Enabled", "ThrottleMinutes", "Channels", "Recipients", 
                           "CreatedAt", "UpdatedAt", "TriggerCount", "Metadata" },
            values: new object[]
            {
                Guid.NewGuid(),
                "High Flakiness Detection",
                "Alert when flaky test count exceeds 5",
                "flakyTestCount",
                "GreaterThan",
                5.0,
                "Error",
                true,
                120,
                "[\"signalr\"]",
                "{}",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                0,
                "{}"
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AlertHistory");
        migrationBuilder.DropTable(name: "AlertRules");
    }
}
```

---

## Migration #2: AddRetentionPolicies

### Purpose
Add data retention/archival fields and audit trail for manual changes.

### Schema Changes

#### 1. **TestExecutionResults** Table - Add Retention Fields

```sql
ALTER TABLE [TestExecutionResults]
ADD [ArchivedAt] DATETIMEOFFSET NULL,
    [RetentionExpiresAt] DATETIMEOFFSET NULL,
    [ArchivalStatus] NVARCHAR(50) NULL DEFAULT 'Active';

CREATE INDEX [IX_TestExecutionResults_ArchivalStatus] 
    ON [TestExecutionResults]([ArchivalStatus]) 
    WHERE [ArchivalStatus] = 'Active';

CREATE INDEX [IX_TestExecutionResults_RetentionExpires] 
    ON [TestExecutionResults]([RetentionExpiresAt]) 
    WHERE [RetentionExpiresAt] IS NOT NULL;
```

**New Properties:**
```csharp
// Add to TestExecutionResult class
public DateTimeOffset? ArchivedAt { get; set; }
public DateTimeOffset? RetentionExpiresAt { get; set; }
public ArchivalStatus ArchivalStatus { get; set; } = ArchivalStatus.Active;

public enum ArchivalStatus
{
    Active,
    PendingArchival,
    Archived,
    Deleted
}
```

---

#### 2. **FlakyTestAnalyses** Table - Add Retention Fields

```sql
ALTER TABLE [FlakyTestAnalyses]
ADD [RetentionExpiresAt] DATETIMEOFFSET NULL,
    [IsSuperseded] BIT NOT NULL DEFAULT 0,
    [SupersededBy] UNIQUEIDENTIFIER NULL;

CREATE INDEX [IX_FlakyTestAnalyses_IsSuperseded] 
    ON [FlakyTestAnalyses]([IsSuperseded]) 
    WHERE [IsSuperseded] = 0;

ALTER TABLE [FlakyTestAnalyses]
ADD CONSTRAINT [FK_FlakyTestAnalyses_SupersededBy]
    FOREIGN KEY ([SupersededBy])
    REFERENCES [FlakyTestAnalyses]([Id])
    ON DELETE NO ACTION;
```

**New Properties:**
```csharp
// Add to FlakyTestAnalysis class
public DateTimeOffset? RetentionExpiresAt { get; set; }
public bool IsSuperseded { get; set; }
public Guid? SupersededBy { get; set; }
```

---

#### 3. **TestTrends** Table - Add Aggregation Support

```sql
ALTER TABLE [TestTrends]
ADD [IsAggregated] BIT NOT NULL DEFAULT 0,
    [AggregatedFrom] NVARCHAR(50) NULL,
    [DataPoints] INT NOT NULL DEFAULT 1;

CREATE INDEX [IX_TestTrends_IsAggregated] 
    ON [TestTrends]([IsAggregated]);
```

**New Properties:**
```csharp
// Add to TestTrend class
public bool IsAggregated { get; set; }
public string? AggregatedFrom { get; set; } // e.g., "Daily" when aggregated to Weekly
public int DataPoints { get; set; } = 1; // Number of raw data points aggregated
```

**Purpose:** Support aggregation strategy where:
- Daily trends kept for 90 days
- After 90 days, daily→ weekly aggregation
- After 1 year, weekly → monthly aggregation

---

#### 4. **RecordingSessions** Table - Add Retention Policy

```sql
ALTER TABLE [RecordingSessions]
ADD [RetentionDays] INT NULL,
    [RetentionExpiresAt] DATETIMEOFFSET NULL,
    [RetentionPolicyApplied] BIT NOT NULL DEFAULT 1;

CREATE INDEX [IX_RecordingSessions_RetentionExpires] 
    ON [RecordingSessions]([RetentionExpiresAt]) 
    WHERE [RetentionExpiresAt] IS NOT NULL;
```

**New Properties:**
```csharp
// Add to RecordingSessionEntity class
public int? RetentionDays { get; set; } // Null = use global default
public DateTimeOffset? RetentionExpiresAt { get; set; }
public bool RetentionPolicyApplied { get; set; } = true;
```

---

#### 5. **NEW: AnalyticsAuditLog** Table

Track manual changes to baselines, alerts, and retention policies.

```csharp
public sealed class AnalyticsAuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public required string EntityType { get; set; } // "AlertRule", "TestTrend", "RetentionPolicy"
    public Guid EntityId { get; set; }
    
    public required string Action { get; set; } // "Created", "Updated", "Deleted", "Archived"
    public required string PerformedBy { get; set; }
    public DateTimeOffset PerformedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

**SQL Schema:**
```sql
CREATE TABLE [AnalyticsAuditLog] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EntityId] UNIQUEIDENTIFIER NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,
    [PerformedBy] NVARCHAR(256) NOT NULL,
    [PerformedAt] DATETIMEOFFSET NOT NULL,
    [OldValues] NVARCHAR(MAX) NULL,
    [NewValues] NVARCHAR(MAX) NULL,
    [Reason] NVARCHAR(1000) NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(500) NULL
);

CREATE INDEX [IX_AnalyticsAuditLog_EntityType_EntityId] 
    ON [AnalyticsAuditLog]([EntityType], [EntityId]);

CREATE INDEX [IX_AnalyticsAuditLog_PerformedAt] 
    ON [AnalyticsAuditLog]([PerformedAt]);

CREATE INDEX [IX_AnalyticsAuditLog_PerformedBy] 
    ON [AnalyticsAuditLog]([PerformedBy]);
```

---

### Migration #2 Code

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddRetentionPolicies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add retention fields to TestExecutionResults
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ArchivedAt",
            table: "TestExecutionResults",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "RetentionExpiresAt",
            table: "TestExecutionResults",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ArchivalStatus",
            table: "TestExecutionResults",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Active");

        // Add retention fields to FlakyTestAnalyses
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "RetentionExpiresAt",
            table: "FlakyTestAnalyses",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsSuperseded",
            table: "FlakyTestAnalyses",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "SupersededBy",
            table: "FlakyTestAnalyses",
            type: "uniqueidentifier",
            nullable: true);

        // Add aggregation support to TestTrends
        migrationBuilder.AddColumn<bool>(
            name: "IsAggregated",
            table: "TestTrends",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "AggregatedFrom",
            table: "TestTrends",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DataPoints",
            table: "TestTrends",
            type: "int",
            nullable: false,
            defaultValue: 1);

        // Add retention policy to RecordingSessions
        migrationBuilder.AddColumn<int>(
            name: "RetentionDays",
            table: "RecordingSessions",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "RetentionExpiresAt",
            table: "RecordingSessions",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "RetentionPolicyApplied",
            table: "RecordingSessions",
            type: "bit",
            nullable: false,
            defaultValue: true);

        // Create AnalyticsAuditLog table
        migrationBuilder.CreateTable(
            name: "AnalyticsAuditLog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PerformedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                PerformedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AnalyticsAuditLog", x => x.Id);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_TestExecutionResults_ArchivalStatus",
            table: "TestExecutionResults",
            column: "ArchivalStatus",
            filter: "[ArchivalStatus] = 'Active'");

        migrationBuilder.CreateIndex(
            name: "IX_TestExecutionResults_RetentionExpires",
            table: "TestExecutionResults",
            column: "RetentionExpiresAt",
            filter: "[RetentionExpiresAt] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_FlakyTestAnalyses_IsSuperseded",
            table: "FlakyTestAnalyses",
            column: "IsSuperseded",
            filter: "[IsSuperseded] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_TestTrends_IsAggregated",
            table: "TestTrends",
            column: "IsAggregated");

        migrationBuilder.CreateIndex(
            name: "IX_RecordingSessions_RetentionExpires",
            table: "RecordingSessions",
            column: "RetentionExpiresAt",
            filter: "[RetentionExpiresAt] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsAuditLog_EntityType_EntityId",
            table: "AnalyticsAuditLog",
            columns: new[] { "EntityType", "EntityId" });

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsAuditLog_PerformedAt",
            table: "AnalyticsAuditLog",
            column: "PerformedAt");

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsAuditLog_PerformedBy",
            table: "AnalyticsAuditLog",
            column: "PerformedBy");

        // Add foreign key for FlakyTestAnalyses supersession
        migrationBuilder.AddForeignKey(
            name: "FK_FlakyTestAnalyses_SupersededBy",
            table: "FlakyTestAnalyses",
            column: "SupersededBy",
            principalTable: "FlakyTestAnalyses",
            principalColumn: "Id",
            onDelete: ReferentialAction.NoAction);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop foreign key
        migrationBuilder.DropForeignKey(
            name: "FK_FlakyTestAnalyses_SupersededBy",
            table: "FlakyTestAnalyses");

        // Drop indexes
        migrationBuilder.DropIndex(name: "IX_TestExecutionResults_ArchivalStatus", table: "TestExecutionResults");
        migrationBuilder.DropIndex(name: "IX_TestExecutionResults_RetentionExpires", table: "TestExecutionResults");
        migrationBuilder.DropIndex(name: "IX_FlakyTestAnalyses_IsSuperseded", table: "FlakyTestAnalyses");
        migrationBuilder.DropIndex(name: "IX_TestTrends_IsAggregated", table: "TestTrends");
        migrationBuilder.DropIndex(name: "IX_RecordingSessions_RetentionExpires", table: "RecordingSessions");

        // Drop columns
        migrationBuilder.DropColumn(name: "ArchivedAt", table: "TestExecutionResults");
        migrationBuilder.DropColumn(name: "RetentionExpiresAt", table: "TestExecutionResults");
        migrationBuilder.DropColumn(name: "ArchivalStatus", table: "TestExecutionResults");
        
        migrationBuilder.DropColumn(name: "RetentionExpiresAt", table: "FlakyTestAnalyses");
        migrationBuilder.DropColumn(name: "IsSuperseded", table: "FlakyTestAnalyses");
        migrationBuilder.DropColumn(name: "SupersededBy", table: "FlakyTestAnalyses");
        
        migrationBuilder.DropColumn(name: "IsAggregated", table: "TestTrends");
        migrationBuilder.DropColumn(name: "AggregatedFrom", table: "TestTrends");
        migrationBuilder.DropColumn(name: "DataPoints", table: "TestTrends");
        
        migrationBuilder.DropColumn(name: "RetentionDays", table: "RecordingSessions");
        migrationBuilder.DropColumn(name: "RetentionExpiresAt", table: "RecordingSessions");
        migrationBuilder.DropColumn(name: "RetentionPolicyApplied", table: "RecordingSessions");

        // Drop table
        migrationBuilder.DropTable(name: "AnalyticsAuditLog");
    }
}
```

---

## Configuration Requirements

Add to `appsettings.json`:

```json
{
  "Analytics": {
    "Alerts": {
      "Enabled": true,
      "DefaultThrottleMinutes": 60,
      "MaxAlertsPerHour": 20,
      "RetainHistoryDays": 90
    },
    "Retention": {
      "Enabled": true,
      "DefaultRetentionDays": 90,
      "TestExecutionResults": {
        "RetentionDays": 90,
        "AggregateAfterDays": 90,
        "DeleteAfterDays": 365
      },
      "TestTrends": {
        "DailyRetentionDays": 90,
        "WeeklyRetentionDays": 365,
        "MonthlyRetentionDays": 1825
      },
      "FlakyTestAnalyses": {
        "MaxAnalysesPerTest": 10,
        "RetentionDays": 180
      },
      "AuditLog": {
        "RetentionDays": 730
      }
    }
  }
}
```

---

## Deployment Strategy

### Phase 1-2 (No Migration)
- Deploy code changes only
- Test with existing schema

### Phase 3 (Add Alerting)
```bash
dotnet ef migrations add AddAnalyticsAlerting --project EvoAITest.Core
dotnet ef database update --project EvoAITest.Core
```

### Phase 4 (Add Retention)
```bash
dotnet ef migrations add AddRetentionPolicies --project EvoAITest.Core
dotnet ef database update --project EvoAITest.Core
```

---

## Rollback Strategy

### Migration #1 Rollback
```bash
dotnet ef database update <PreviousMigration> --project EvoAITest.Core
dotnet ef migrations remove --project EvoAITest.Core
```

**Impact:** Alerts stop working but no data loss on existing analytics.

### Migration #2 Rollback
```bash
dotnet ef database update AddAnalyticsAlerting --project EvoAITest.Core
dotnet ef migrations remove --project EvoAITest.Core
```

**Impact:** Retention policies disabled, audit log lost. No impact on existing test data.

---

## Testing Strategy

### Unit Tests
- Test alert condition evaluation
- Test retention policy calculation
- Test audit log serialization

### Integration Tests
- Create alert → Trigger → Verify history
- Apply retention → Verify archival status
- Audit log tracking on CRUD operations

### Performance Tests
- Query 100k+ alert history records
- Archival operation on 1M+ test results
- Index usage verification

---

## Summary

**Migration #1: AddAnalyticsAlerting**
- Tables: 2 (AlertRules, AlertHistory)
- Indexes: 12
- Seed Data: 2 default alert rules
- Estimated Rows: 100 rules, 10k history entries

**Migration #2: AddRetentionPolicies**
- Tables: 1 new (AnalyticsAuditLog)
- Column Additions: 13 across 4 tables
- Indexes: 6
- Estimated Impact: Minimal (nullable columns)

**Total Database Impact:**
- 3 new tables
- 13 new columns
- 18 new indexes
- ~2 KB per alert rule
- ~1 KB per alert history entry
- ~500 bytes per audit log entry

**Ready for Implementation:** ✅ YES
