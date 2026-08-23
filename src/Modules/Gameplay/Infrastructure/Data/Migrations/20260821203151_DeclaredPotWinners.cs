using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameplay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeclaredPotWinners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hand_pot_winners",
                schema: "gameplay",
                columns: table => new
                {
                    pot_index = table.Column<int>(type: "integer", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hand_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_pot_winners", x => new { x.hand_id, x.pot_index, x.participant_id });
                    table.ForeignKey(
                        name: "fk_hand_pot_winners_hands_hand_id",
                        column: x => x.hand_id,
                        principalSchema: "gameplay",
                        principalTable: "hands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_hand_pot_winners_participants_participant_id",
                        column: x => x.participant_id,
                        principalSchema: "gameplay",
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hand_pot_winners_participant_id",
                schema: "gameplay",
                table: "hand_pot_winners",
                column: "participant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hand_pot_winners",
                schema: "gameplay");
        }
    }
}
