using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPortfolioTracker.Migrations.Coin
{
    /// <inheritdoc />
    public partial class AddedNoteToCoinEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Coins",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Coins");
        }
    }
}
