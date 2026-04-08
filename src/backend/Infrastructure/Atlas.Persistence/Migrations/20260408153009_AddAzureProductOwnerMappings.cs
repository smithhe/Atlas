using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAzureProductOwnerMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AzureProductOwnerMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AzureUniqueName = table.Column<string>(type: "text", nullable: false),
                    ProductOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureProductOwnerMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzureProductOwnerMappings_ProductOwners_ProductOwnerId",
                        column: x => x.ProductOwnerId,
                        principalTable: "ProductOwners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AzureProductOwnerMappings_AzureUniqueName",
                table: "AzureProductOwnerMappings",
                column: "AzureUniqueName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzureProductOwnerMappings_ProductOwnerId",
                table: "AzureProductOwnerMappings",
                column: "ProductOwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AzureProductOwnerMappings");
        }
    }
}
