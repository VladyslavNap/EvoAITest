using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvoAITest.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            // Create indexes for FlakyTestAnalyses
            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_RecordingSessionId",
                table: "FlakyTestAnalyses",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_Severity",
                table: "FlakyTestAnalyses",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_FlakinessScore",
                table: "FlakyTestAnalyses",
                column: "FlakinessScore");

            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_AnalyzedAt",
                table: "FlakyTestAnalyses",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FlakyTestAnalyses_RecordingId_Score",
                table: "FlakyTestAnalyses",
                columns: new[] { "RecordingSessionId", "FlakinessScore" });

            // Create indexes for TestTrends
            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_Timestamp",
                table: "TestTrends",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_RecordingSessionId",
                table: "TestTrends",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_Interval",
                table: "TestTrends",
                column: "Interval");

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_RecordingId_Timestamp",
                table: "TestTrends",
                columns: new[] { "RecordingSessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_Interval_Timestamp",
                table: "TestTrends",
                columns: new[] { "Interval", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TestTrends_RecordingId_Interval_Timestamp",
                table: "TestTrends",
                columns: new[] { "RecordingSessionId", "Interval", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlakyTestAnalyses");

            migrationBuilder.DropTable(
                name: "TestTrends");
        }
    }
}
