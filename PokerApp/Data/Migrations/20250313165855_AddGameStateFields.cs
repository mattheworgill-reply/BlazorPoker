using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddGameStateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BetsMade",
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
                name: "CurrentPotAmount",
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

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBet",
                table: "GamePlayers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HandCardsJson",
                table: "GamePlayers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasFolded",
                table: "GamePlayers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDealer",
                table: "GamePlayers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInGame",
                table: "GamePlayers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PokerPosition",
                table: "GamePlayers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BetsMade",
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
                name: "CurrentPotAmount",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "CurrentTurnPlayerPosition",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "CurrentBet",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "HandCardsJson",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "HasFolded",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "IsDealer",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "IsInGame",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "PokerPosition",
                table: "GamePlayers");
        }
    }
}
