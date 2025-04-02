using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTimerSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeRemaining",
                table: "PokerTables",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeRemaining",
                table: "PokerTables");
        }
    }
}
