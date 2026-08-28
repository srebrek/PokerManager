using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Statistics.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "statistics");

            migrationBuilder.CreateTable(
                name: "hand",
                schema: "statistics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    aborted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rebuy",
                schema: "statistics",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rebuy", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "hand_action",
                schema: "statistics",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount_to = table.Column<int>(type: "integer", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    undone_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hand_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_action", x => new { x.event_id, x.sequence_number });
                    table.ForeignKey(
                        name: "fk_hand_action_hand_hand_id",
                        column: x => x.hand_id,
                        principalSchema: "statistics",
                        principalTable: "hand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hand_pot",
                schema: "statistics",
                columns: table => new
                {
                    pot_index = table.Column<int>(type: "integer", nullable: false),
                    hand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_pot", x => new { x.hand_id, x.pot_index });
                    table.ForeignKey(
                        name: "fk_hand_pot_hand_hand_id",
                        column: x => x.hand_id,
                        principalSchema: "statistics",
                        principalTable: "hand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hand_result",
                schema: "statistics",
                columns: table => new
                {
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_name = table.Column<string>(type: "text", nullable: false),
                    net = table.Column<int>(type: "integer", nullable: false),
                    ending_chips = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_result", x => new { x.hand_id, x.participant_id });
                    table.ForeignKey(
                        name: "fk_hand_result_hand_hand_id",
                        column: x => x.hand_id,
                        principalSchema: "statistics",
                        principalTable: "hand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hand_seat",
                schema: "statistics",
                columns: table => new
                {
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    starting_chips = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_seat", x => new { x.hand_id, x.participant_id });
                    table.ForeignKey(
                        name: "fk_hand_seat_hand_hand_id",
                        column: x => x.hand_id,
                        principalSchema: "statistics",
                        principalTable: "hand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hand_pot_winner",
                schema: "statistics",
                columns: table => new
                {
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pot_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_pot_winner", x => new { x.hand_id, x.pot_index, x.participant_id });
                    table.ForeignKey(
                        name: "fk_hand_pot_winner_hand_pot_hand_id_pot_index",
                        columns: x => new { x.hand_id, x.pot_index },
                        principalSchema: "statistics",
                        principalTable: "hand_pot",
                        principalColumns: new[] { "hand_id", "pot_index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hand_action_hand_id",
                schema: "statistics",
                table: "hand_action",
                column: "hand_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hand_action",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "hand_pot_winner",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "hand_result",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "hand_seat",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "rebuy",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "hand_pot",
                schema: "statistics");

            migrationBuilder.DropTable(
                name: "hand",
                schema: "statistics");
        }
    }
}
