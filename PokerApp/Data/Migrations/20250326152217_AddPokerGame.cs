using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPokerGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PokerTables_AspNetUsers_OwnerId",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "BetsMade",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "BlindOption",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "CommunityCardsJson",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "CurrentBetAmount",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "CurrentDealerPosition",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "CurrentGameState",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "CurrentRoundPotAmount",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "CurrentTurnPlayerPosition",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "DeckJson",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "EndPlayerPosition",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "PotJson",
                table: "PokerTables");

            migrationBuilder.AddColumn<string>(
                name: "playerLeaves",
                table: "PokerTables",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "PokerGames",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TableId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CurrentTurnPlayerPosition = table.Column<int>(type: "int", nullable: false),
                    CurrentDealerPosition = table.Column<int>(type: "int", nullable: false),
                    EndPlayerPosition = table.Column<int>(type: "int", nullable: false),
                    CurrentBetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentRoundPotAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BlindOption = table.Column<bool>(type: "bit", nullable: false),
                    BetsMade = table.Column<bool>(type: "bit", nullable: false),
                    CommunityCardsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeckJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlayerIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokerGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokerGames_PokerTables_TableId",
                        column: x => x.TableId,
                        principalTable: "PokerTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokerGames_TableId",
                table: "PokerGames",
                column: "TableId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PokerTables_AspNetUsers_OwnerId",
                table: "PokerTables",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PokerTables_AspNetUsers_OwnerId",
                table: "PokerTables");

            migrationBuilder.DropTable(
                name: "PokerGames");

            migrationBuilder.DropColumn(
                name: "playerLeaves",
                table: "PokerTables");

            migrationBuilder.AddColumn<bool>(
                name: "BetsMade",
                table: "PokerTables",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BlindOption",
                table: "PokerTables",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CommunityCardsJson",
                table: "PokerTables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBetAmount",
                table: "PokerTables",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrentDealerPosition",
                table: "PokerTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentGameState",
                table: "PokerTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentRoundPotAmount",
                table: "PokerTables",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrentTurnPlayerPosition",
                table: "PokerTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeckJson",
                table: "PokerTables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndPlayerPosition",
                table: "PokerTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PotJson",
                table: "PokerTables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PokerTables_AspNetUsers_OwnerId",
                table: "PokerTables",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
