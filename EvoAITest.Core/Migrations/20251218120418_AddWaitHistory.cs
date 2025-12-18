using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvoAITest.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "WaitHistory");
        }
    }
}
