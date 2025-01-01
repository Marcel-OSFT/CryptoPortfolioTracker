using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPortfolioTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddOneToManyToCoinEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coins_Narratives_NarrativeId",
                table: "Coins");

            migrationBuilder.AlterColumn<int>(
                name: "NarrativeId",
                table: "Coins",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Coins_Narratives_NarrativeId",
                table: "Coins",
                column: "NarrativeId",
                principalTable: "Narratives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coins_Narratives_NarrativeId",
                table: "Coins");

            migrationBuilder.AlterColumn<int>(
                name: "NarrativeId",
                table: "Coins",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Coins_Narratives_NarrativeId",
                table: "Coins",
                column: "NarrativeId",
                principalTable: "Narratives",
                principalColumn: "Id");
        }
    }
}
