using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPortfolioTracker.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedCoinEntity1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAddedToAssets",
                table: "Coins",
                newName: "IsAsset");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAsset",
                table: "Coins",
                newName: "IsAddedToAssets");
        }
    }
}
