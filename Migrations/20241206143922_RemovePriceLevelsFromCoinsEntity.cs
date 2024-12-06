using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPortfolioTracker.Migrations
{
    /// <inheritdoc />
    public partial class RemovePriceLevelsFromCoinsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyLevel",
                table: "Coins");

            migrationBuilder.DropColumn(
                name: "StopLevel",
                table: "Coins");

            migrationBuilder.DropColumn(
                name: "TakeProfitLevel",
                table: "Coins");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BuyLevel",
                table: "Coins",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
 
            migrationBuilder.AddColumn<double>(
                name: "StopLevel",
                table: "Coins",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TakeProfitLevel",
                table: "Coins",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

        }
    }
}
