using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvoAITest.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualRegressionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VisualCheckpointsFailed",
                table: "ExecutionHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisualCheckpointsPassed",
                table: "ExecutionHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VisualComparisonResults",
                table: "ExecutionHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisualStatus",
                table: "ExecutionHistory",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "AutomationTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisualCheckpoints",
                table: "AutomationTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisualComparisonResults");

            migrationBuilder.DropTable(
                name: "VisualBaselines");

            migrationBuilder.DropColumn(
                name: "VisualCheckpointsFailed",
                table: "ExecutionHistory");

            migrationBuilder.DropColumn(
                name: "VisualCheckpointsPassed",
                table: "ExecutionHistory");

            migrationBuilder.DropColumn(
                name: "VisualComparisonResults",
                table: "ExecutionHistory");

            migrationBuilder.DropColumn(
                name: "VisualStatus",
                table: "ExecutionHistory");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "AutomationTasks");

            migrationBuilder.DropColumn(
                name: "VisualCheckpoints",
                table: "AutomationTasks");
        }
    }
}
