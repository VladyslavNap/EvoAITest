using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvoAITest.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoveryHistory");
        }
    }
}
