using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "AiSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "TurnIndex",
                table: "AiSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AiConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    View = table.Column<string>(type: "text", nullable: false),
                    ActionId = table.Column<string>(type: "text", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    RiskId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiSessions_ConversationId",
                table: "AiSessions",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSessions_ConversationId_TurnIndex",
                table: "AiSessions",
                columns: new[] { "ConversationId", "TurnIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_ProjectId",
                table: "AiConversations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_RiskId",
                table: "AiConversations",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_TaskId",
                table: "AiConversations",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_TeamMemberId",
                table: "AiConversations",
                column: "TeamMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_UpdatedAtUtc",
                table: "AiConversations",
                column: "UpdatedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_AiSessions_AiConversations_ConversationId",
                table: "AiSessions",
                column: "ConversationId",
                principalTable: "AiConversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiSessions_AiConversations_ConversationId",
                table: "AiSessions");

            migrationBuilder.DropTable(
                name: "AiConversations");

            migrationBuilder.DropIndex(
                name: "IX_AiSessions_ConversationId",
                table: "AiSessions");

            migrationBuilder.DropIndex(
                name: "IX_AiSessions_ConversationId_TurnIndex",
                table: "AiSessions");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "AiSessions");

            migrationBuilder.DropColumn(
                name: "TurnIndex",
                table: "AiSessions");
        }
    }
}
