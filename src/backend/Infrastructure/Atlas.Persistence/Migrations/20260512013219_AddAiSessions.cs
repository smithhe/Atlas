using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    View = table.Column<string>(type: "text", nullable: false),
                    ActionId = table.Column<string>(type: "text", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    RiskId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsTerminal = table.Column<string>(type: "character varying(5)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiSessionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AiSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Delta = table.Column<string>(type: "text", nullable: true),
                    IsTerminal = table.Column<string>(type: "character varying(5)", nullable: false),
                    ToolName = table.Column<string>(type: "text", nullable: true),
                    ToolCallId = table.Column<string>(type: "text", nullable: true),
                    ArgumentsJson = table.Column<string>(type: "text", nullable: true),
                    ResultJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSessionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiSessionEvents_AiSessions_AiSessionId",
                        column: x => x.AiSessionId,
                        principalTable: "AiSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiSessionEvents_AiSessionId",
                table: "AiSessionEvents",
                column: "AiSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSessionEvents_AiSessionId_OccurredAtUtc",
                table: "AiSessionEvents",
                columns: new[] { "AiSessionId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiSessionEvents_AiSessionId_Sequence",
                table: "AiSessionEvents",
                columns: new[] { "AiSessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiSessions_CreatedAtUtc",
                table: "AiSessions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiSessions_ProjectId",
                table: "AiSessions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSessions_RiskId",
                table: "AiSessions",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSessions_TaskId",
                table: "AiSessions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSessions_TeamMemberId",
                table: "AiSessions",
                column: "TeamMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiSessionEvents");

            migrationBuilder.DropTable(
                name: "AiSessions");
        }
    }
}
