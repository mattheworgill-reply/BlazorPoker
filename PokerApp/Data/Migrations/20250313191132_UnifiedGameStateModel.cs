using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerApp.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedGameStateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "GamePlayers");

            migrationBuilder.RenameColumn(
                name: "CurrentPotAmount",
                table: "PokerTables",
                newName: "CurrentRoundPotAmount");

            migrationBuilder.AddColumn<bool>(
                name: "BlindOption",
                table: "PokerTables",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<string>(
                name: "TablePositionsJson",
                table: "PokerTables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "GamePlayers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlindOption",
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

            migrationBuilder.DropColumn(
                name: "TablePositionsJson",
                table: "PokerTables");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "GamePlayers");

            migrationBuilder.RenameColumn(
                name: "CurrentRoundPotAmount",
                table: "PokerTables",
                newName: "CurrentPotAmount");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "GamePlayers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
