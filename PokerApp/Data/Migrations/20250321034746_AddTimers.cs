using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTimers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGamePaused",
                table: "PokerTables",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGamePaused",
                table: "PokerTables");
        }
    }
}
