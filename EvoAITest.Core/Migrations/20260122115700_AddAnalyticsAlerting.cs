using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvoAITest.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsAlerting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    RecordingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ThrottleMinutes = table.Column<int>(type: "int", nullable: false),
                    Channels = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastTriggeredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TriggerCount = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlakyTestAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FlakinessScore = table.Column<double>(type: "float", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalExecutions = table.Column<int>(type: "int", nullable: false),
                    FlakyFailureCount = table.Column<int>(type: "int", nullable: false),
                    ConsistentPassCount = table.Column<int>(type: "int", nullable: false),
                    ConsistentFailureCount = table.Column<int>(type: "int", nullable: false),
                    PassRate = table.Column<double>(type: "float", nullable: false),
                    DurationVariability = table.Column<double>(type: "float", nullable: false),
                    Patterns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastExecutionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RootCauses = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisConfidence = table.Column<double>(type: "float", nullable: false),
                    AverageTimeToFailure = table.Column<long>(type: "bigint", nullable: true),
                    ExecutionDurationStdDev = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlakyTestAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecordingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StartUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Browser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ViewportWidth = table.Column<int>(type: "int", nullable: false),
                    ViewportHeight = table.Column<int>(type: "int", nullable: false),
                    GeneratedTestCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestFramework = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetricsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestExecutionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TestFramework = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StandardOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepResults = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Artifacts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompilationErrors = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestExecutionResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestExecutionSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TestCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestFramework = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    TotalSteps = table.Column<int>(type: "int", nullable: false),
                    StepResults = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Artifacts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsoleOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestExecutionSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestTrends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TestName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Interval = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalExecutions = table.Column<int>(type: "int", nullable: false),
                    PassedExecutions = table.Column<int>(type: "int", nullable: false),
                    FailedExecutions = table.Column<int>(type: "int", nullable: false),
                    SkippedExecutions = table.Column<int>(type: "int", nullable: false),
                    PassRate = table.Column<double>(type: "float", nullable: false),
                    AverageDurationMs = table.Column<long>(type: "bigint", nullable: false),
                    MinDurationMs = table.Column<long>(type: "bigint", nullable: false),
                    MaxDurationMs = table.Column<long>(type: "bigint", nullable: false),
                    DurationStdDev = table.Column<long>(type: "bigint", nullable: false),
                    FlakyTestCount = table.Column<int>(type: "int", nullable: false),
                    UniqueTestCount = table.Column<int>(type: "int", nullable: false),
                    CompilationErrors = table.Column<int>(type: "int", nullable: false),
                    RetriedTests = table.Column<int>(type: "int", nullable: false),
                    AverageStepsPerTest = table.Column<double>(type: "float", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Metrics = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTrends", x => x.Id);
                });

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
                    Acknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcknowledgmentNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationSuccess = table.Column<bool>(type: "bit", nullable: false),
                    NotificationError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertHistory_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecordedInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    InputValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CoordinateX = table.Column<int>(type: "int", nullable: true),
                    CoordinateY = table.Column<int>(type: "int", nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncludeInTest = table.Column<bool>(type: "bit", nullable: false),
                    IntentConfidence = table.Column<double>(type: "float", nullable: false),
                    GeneratedCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssertionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordedInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordedInteractions_RecordingSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "RecordingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistory_Acknowledged",
                table: "AlertHistory",
                column: "Acknowledged",
                filter: "[Acknowledged] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistory_AlertRuleId",
                table: "AlertHistory",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistory_RecordingSessionId",
                table: "AlertHistory",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistory_RuleId_Triggered",
                table: "AlertHistory",
                columns: new[] { "AlertRuleId", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistory_Severity",
                table: "AlertHistory",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistory_TriggeredAt",
                table: "AlertHistory",
                column: "TriggeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_Enabled",
                table: "AlertRules",
                column: "Enabled",
                filter: "[Enabled] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_Enabled_Metric",
                table: "AlertRules",
                columns: new[] { "Enabled", "Metric" },
                filter: "[Enabled] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_LastTriggered",
                table: "AlertRules",
                column: "LastTriggeredAt");

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
                name: "IX_FlakyTestAnalyses_AnalyzedAt",
                table: "FlakyTestAnalyses",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_FlakinessScore",
                table: "FlakyTestAnalyses",
                column: "FlakinessScore");

            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_RecordingId_Score",
                table: "FlakyTestAnalyses",
                columns: new[] { "RecordingSessionId", "FlakinessScore" });

            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_RecordingSessionId",
                table: "FlakyTestAnalyses",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_Severity",
                table: "FlakyTestAnalyses",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_RecordedInteractions_ActionType",
                table: "RecordedInteractions",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_RecordedInteractions_SessionId",
                table: "RecordedInteractions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordedInteractions_SessionId_Sequence",
                table: "RecordedInteractions",
                columns: new[] { "SessionId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RecordedInteractions_Timestamp",
                table: "RecordedInteractions",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_RecordingSessions_CreatedBy",
                table: "RecordingSessions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RecordingSessions_StartedAt",
                table: "RecordingSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecordingSessions_Status",
                table: "RecordingSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RecordingSessions_Status_StartedAt",
                table: "RecordingSessions",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_RecordingSessionId",
                table: "TestExecutionResults",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_RecordingSessionId_Status",
                table: "TestExecutionResults",
                columns: new[] { "RecordingSessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_StartedAt",
                table: "TestExecutionResults",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_Status",
                table: "TestExecutionResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_Status_StartedAt",
                table: "TestExecutionResults",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionSessions_RecordingSessionId",
                table: "TestExecutionSessions",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionSessions_StartedAt",
                table: "TestExecutionSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionSessions_Status",
                table: "TestExecutionSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_Interval",
                table: "TestTrends",
                column: "Interval");

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_Interval_Timestamp",
                table: "TestTrends",
                columns: new[] { "Interval", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_RecordingId_Interval_Timestamp",
                table: "TestTrends",
                columns: new[] { "RecordingSessionId", "Interval", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_RecordingId_Timestamp",
                table: "TestTrends",
                columns: new[] { "RecordingSessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_RecordingSessionId",
                table: "TestTrends",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_Timestamp",
                table: "TestTrends",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertHistory");

            migrationBuilder.DropTable(
                name: "FlakyTestAnalyses");

            migrationBuilder.DropTable(
                name: "RecordedInteractions");

            migrationBuilder.DropTable(
                name: "TestExecutionResults");

            migrationBuilder.DropTable(
                name: "TestExecutionSessions");

            migrationBuilder.DropTable(
                name: "TestTrends");

            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "RecordingSessions");
        }
    }
}
