using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace CryptoPortfolioTracker.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedCoinEntity_IsAsset : Migration
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
                newName: "IsAddedAssets");
        }
    }
}
