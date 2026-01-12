using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvoAITest.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTestExecutionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_RecordingSessionId",
                table: "TestExecutionResults",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_Status",
                table: "TestExecutionResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_StartedAt",
                table: "TestExecutionResults",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_RecordingSessionId_Status",
                table: "TestExecutionResults",
                columns: new[] { "RecordingSessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionResults_Status_StartedAt",
                table: "TestExecutionResults",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionSessions_RecordingSessionId",
                table: "TestExecutionSessions",
                column: "RecordingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionSessions_Status",
                table: "TestExecutionSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionSessions_StartedAt",
                table: "TestExecutionSessions",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestExecutionResults");

            migrationBuilder.DropTable(
                name: "TestExecutionSessions");
        }
    }
}
