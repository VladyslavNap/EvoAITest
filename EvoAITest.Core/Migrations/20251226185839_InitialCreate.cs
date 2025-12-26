using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvoAITest.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomationTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NaturalLanguagePrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VisualCheckpoints = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionStatus = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    StepResults = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScreenshotUrls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisualComparisonResults = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisualCheckpointsPassed = table.Column<int>(type: "int", nullable: false),
                    VisualCheckpointsFailed = table.Column<int>(type: "int", nullable: false),
                    VisualStatus = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionHistory_AutomationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecoveryStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecoveryActions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    RecoveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Selector = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryHistory_AutomationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelectorHealingHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalSelector = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HealedSelector = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HealingStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    HealedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpectedText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectorHealingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectorHealingHistory_AutomationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisualBaselines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckpointName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Browser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Viewport = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BaselinePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GitCommit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GitBranch = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BuildVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreviousBaselineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdateReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisualBaselines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisualBaselines_AutomationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisualBaselines_VisualBaselines_PreviousBaselineId",
                        column: x => x.PreviousBaselineId,
                        principalTable: "VisualBaselines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WaitHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Selector = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WaitCondition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TimeoutMs = table.Column<int>(type: "int", nullable: false),
                    ActualWaitMs = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitHistory_AutomationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisualComparisonResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckpointName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaselineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaselinePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiffPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DifferencePercentage = table.Column<double>(type: "float", nullable: false),
                    Tolerance = table.Column<double>(type: "float", nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    PixelsDifferent = table.Column<int>(type: "int", nullable: false),
                    TotalPixels = table.Column<int>(type: "int", nullable: false),
                    SsimScore = table.Column<double>(type: "float", nullable: true),
                    DifferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Regions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComparedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisualComparisonResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisualComparisonResults_AutomationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisualComparisonResults_ExecutionHistory_ExecutionHistoryId",
                        column: x => x.ExecutionHistoryId,
                        principalTable: "ExecutionHistory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VisualComparisonResults_VisualBaselines_BaselineId",
                        column: x => x.BaselineId,
                        principalTable: "VisualBaselines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationTasks_CreatedAt",
                table: "AutomationTasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationTasks_Status",
                table: "AutomationTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationTasks_UserId",
                table: "AutomationTasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationTasks_UserId_Status",
                table: "AutomationTasks",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionHistory_ExecutionStatus",
                table: "ExecutionHistory",
                column: "ExecutionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionHistory_StartedAt",
                table: "ExecutionHistory",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionHistory_TaskId",
                table: "ExecutionHistory",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryHistory_ErrorType",
                table: "RecoveryHistory",
                column: "ErrorType");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryHistory_ErrorType_Success",
                table: "RecoveryHistory",
                columns: new[] { "ErrorType", "Success" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryHistory_RecoveredAt",
                table: "RecoveryHistory",
                column: "RecoveredAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryHistory_Success",
                table: "RecoveryHistory",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryHistory_TaskId",
                table: "RecoveryHistory",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SelectorHealingHistory_HealedAt",
                table: "SelectorHealingHistory",
                column: "HealedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SelectorHealingHistory_HealingStrategy",
                table: "SelectorHealingHistory",
                column: "HealingStrategy");

            migrationBuilder.CreateIndex(
                name: "IX_SelectorHealingHistory_Success",
                table: "SelectorHealingHistory",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_SelectorHealingHistory_TaskId",
                table: "SelectorHealingHistory",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_VisualBaselines_CreatedAt",
                table: "VisualBaselines",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VisualBaselines_GitBranch",
                table: "VisualBaselines",
                column: "GitBranch");

            migrationBuilder.CreateIndex(
                name: "IX_VisualBaselines_PreviousBaselineId",
                table: "VisualBaselines",
                column: "PreviousBaselineId");

            migrationBuilder.CreateIndex(
                name: "IX_VisualBaselines_TaskId_CheckpointName_Environment_Browser",
                table: "VisualBaselines",
                columns: new[] { "TaskId", "CheckpointName", "Environment", "Browser" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisualBaselines_TaskId_CheckpointName_Environment_Browser_Viewport",
                table: "VisualBaselines",
                columns: new[] { "TaskId", "CheckpointName", "Environment", "Browser", "Viewport" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisualComparisonResults_BaselineId",
                table: "VisualComparisonResults",
                column: "BaselineId");

            migrationBuilder.CreateIndex(
                name: "IX_VisualComparisonResults_ComparedAt",
                table: "VisualComparisonResults",
                column: "ComparedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VisualComparisonResults_ExecutionHistoryId",
                table: "VisualComparisonResults",
                column: "ExecutionHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VisualComparisonResults_Passed",
                table: "VisualComparisonResults",
                column: "Passed");

            migrationBuilder.CreateIndex(
                name: "IX_VisualComparisonResults_TaskId",
                table: "VisualComparisonResults",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitHistory_Action",
                table: "WaitHistory",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_WaitHistory_RecordedAt",
                table: "WaitHistory",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WaitHistory_Success",
                table: "WaitHistory",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_WaitHistory_TaskId",
                table: "WaitHistory",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoveryHistory");

            migrationBuilder.DropTable(
                name: "SelectorHealingHistory");

            migrationBuilder.DropTable(
                name: "VisualComparisonResults");

            migrationBuilder.DropTable(
                name: "WaitHistory");

            migrationBuilder.DropTable(
                name: "ExecutionHistory");

            migrationBuilder.DropTable(
                name: "VisualBaselines");

            migrationBuilder.DropTable(
                name: "AutomationTasks");
        }
    }
}
