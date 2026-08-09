using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameplay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStreetFromHand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "street",
                schema: "gameplay",
                table: "hands");

            migrationBuilder.DropColumn(
                name: "street",
                schema: "gameplay",
                table: "hand_actions");

            migrationBuilder.RenameColumn(
                name: "amount",
                schema: "gameplay",
                table: "hand_actions",
                newName: "amount_to");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "amount_to",
                schema: "gameplay",
                table: "hand_actions",
                newName: "amount");

            migrationBuilder.AddColumn<string>(
                name: "street",
                schema: "gameplay",
                table: "hands",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "street",
                schema: "gameplay",
                table: "hand_actions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
