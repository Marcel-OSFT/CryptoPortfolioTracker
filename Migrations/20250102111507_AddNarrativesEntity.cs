using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPortfolioTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrativesEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Narratives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    About = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Narratives", x => x.Id);
                });

            // Insert initial data into the Narratives table
            migrationBuilder.Sql("INSERT INTO Narratives (Name, About) VALUES ('- Not Assigned -', 'Default setting in case you do not want to assign narratives');");

            migrationBuilder.AddColumn<int>(
                name: "NarrativeId",
                table: "Coins",
                type: "INTEGER",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Coins_NarrativeId",
                table: "Coins",
                column: "NarrativeId");

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

            migrationBuilder.DropIndex(
                name: "IX_Coins_NarrativeId",
                table: "Coins");

            migrationBuilder.DropColumn(
                name: "NarrativeId",
                table: "Coins");

            // Delete the initial data from the Narratives table
            migrationBuilder.Sql("DELETE FROM Narratives WHERE Name IN ('- Not Assigned -');");


            migrationBuilder.DropTable(
                name: "Narratives");

            
        }
    }
}
