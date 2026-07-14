using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameplay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gameplay");

            migrationBuilder.CreateTable(
                name: "games",
                schema: "gameplay",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    join_code = table.Column<string>(type: "text", nullable: false),
                    host_participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    small_blind = table.Column<int>(type: "integer", nullable: false),
                    big_blind = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_games", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hands",
                schema: "gameplay",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hands", x => x.id);
                    table.ForeignKey(
                        name: "fk_hands_games_game_id",
                        column: x => x.game_id,
                        principalSchema: "gameplay",
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                schema: "gameplay",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    chips = table.Column<int>(type: "integer", nullable: false),
                    sitting_out = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_participants_games_game_id",
                        column: x => x.game_id,
                        principalSchema: "gameplay",
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hand_actions",
                schema: "gameplay",
                columns: table => new
                {
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    hand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: true),
                    street = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_actions", x => new { x.hand_id, x.sequence_number });
                    table.ForeignKey(
                        name: "fk_hand_actions_hands_hand_id",
                        column: x => x.hand_id,
                        principalSchema: "gameplay",
                        principalTable: "hands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_hand_actions_participants_participant_id",
                        column: x => x.participant_id,
                        principalSchema: "gameplay",
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hand_seats",
                schema: "gameplay",
                columns: table => new
                {
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starting_stack = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_seats", x => new { x.hand_id, x.participant_id });
                    table.ForeignKey(
                        name: "fk_hand_seats_hands_hand_id",
                        column: x => x.hand_id,
                        principalSchema: "gameplay",
                        principalTable: "hands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_hand_seats_participants_participant_id",
                        column: x => x.participant_id,
                        principalSchema: "gameplay",
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hand_actions_participant_id",
                schema: "gameplay",
                table: "hand_actions",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "ix_hand_seats_participant_id",
                schema: "gameplay",
                table: "hand_seats",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "ix_hands_game_id",
                schema: "gameplay",
                table: "hands",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_game_id",
                schema: "gameplay",
                table: "participants",
                column: "game_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hand_actions",
                schema: "gameplay");

            migrationBuilder.DropTable(
                name: "hand_seats",
                schema: "gameplay");

            migrationBuilder.DropTable(
                name: "hands",
                schema: "gameplay");

            migrationBuilder.DropTable(
                name: "participants",
                schema: "gameplay");

            migrationBuilder.DropTable(
                name: "games",
                schema: "gameplay");
        }
    }
}
