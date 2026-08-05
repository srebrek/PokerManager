using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameplay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayableGameShape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sitting_out",
                schema: "gameplay",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "gameplay",
                table: "participants");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "gameplay",
                table: "games");

            migrationBuilder.AddColumn<bool>(
                name: "is_finished",
                schema: "gameplay",
                table: "games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid[]>(
                name: "seating_order",
                schema: "gameplay",
                table: "games",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_finished",
                schema: "gameplay",
                table: "games");

            migrationBuilder.DropColumn(
                name: "seating_order",
                schema: "gameplay",
                table: "games");

            migrationBuilder.AddColumn<bool>(
                name: "sitting_out",
                schema: "gameplay",
                table: "participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                schema: "gameplay",
                table: "participants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "gameplay",
                table: "games",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
