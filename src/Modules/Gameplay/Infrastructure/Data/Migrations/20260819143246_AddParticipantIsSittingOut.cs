using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameplay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantIsSittingOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_sitting_out",
                schema: "gameplay",
                table: "participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_sitting_out",
                schema: "gameplay",
                table: "participants");
        }
    }
}
