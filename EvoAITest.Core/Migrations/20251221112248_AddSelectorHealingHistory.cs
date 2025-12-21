using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvoAITest.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectorHealingHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SelectorHealingHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WaitHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Selector = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaitCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeoutMs = table.Column<int>(type: "int", nullable: false),
                    ActualWaitMs = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "IX_WaitHistory_TaskId",
                table: "WaitHistory",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SelectorHealingHistory");

            migrationBuilder.DropIndex(
                name: "IX_WaitHistory_TaskId",
                table: "WaitHistory");
        }
    }
}
