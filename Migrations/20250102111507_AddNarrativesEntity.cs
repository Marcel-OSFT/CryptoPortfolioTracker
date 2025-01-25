using CryptoPortfolioTracker.Services;
using Flurl.Util;
using LanguageExt.SomeHelp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

#nullable disable

namespace CryptoPortfolioTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrativesEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if migration "20241228225250_AddNarrativesEntity" has been applied

            var service = App.Container.GetService<PortfolioService>();

            var appliedMigrations = service.Context.Database.GetAppliedMigrations().ToList();//.Contains("20241228225250_AddNarrativesEntity");

            var isApplied = appliedMigrations.Contains("20241228225250_AddNarrativesEntity");

            if (isApplied)
            {
                // Insert initial data into the Narratives table
                migrationBuilder.Sql("INSERT INTO Narratives (Name, About) VALUES ('- Not Assigned -', 'Default setting in case you do not want to assign narratives');");

                // Get the ID of the inserted default narrative
                migrationBuilder.Sql("UPDATE Coins SET NarrativeId = (SELECT Id FROM Narratives WHERE Name = '- Not Assigned -') WHERE NarrativeId IS NULL");
            }
            else
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
            migrationBuilder.Sql(@"
                INSERT INTO Narratives (Name, About) VALUES 
                ('AI', 'AI in crypto refers to the use of artificial intelligence to optimize trading, provide market insights, and enhance security.'),
                ('Appchain', 'Appchains are application-specific blockchains designed to optimize performance for particular decentralized applications (DApps).'),
                ('DeFi', 'DeFi (Decentralized Finance) aims to recreate traditional financial systems using decentralized technologies like blockchain.'),
                ('DEX', 'DEX (Decentralized Exchange) allows users to trade cryptocurrencies directly without an intermediary, leveraging smart contracts.'),
                ('DePin', 'DePin (Decentralized Physical Infrastructure Networks) combines blockchain with physical infrastructures like IoT to create decentralized networks.'),
                ('Domains', 'Blockchain domains offer decentralized, censorship-resistant alternatives to traditional domain names, enhancing ownership and control.'),
                ('Gamble-Fi', 'Gamble-Fi integrates decentralized finance principles with online gambling, providing transparent and secure gaming experiences.'),
                ('Game-Fi', 'Game-Fi combines gaming and decentralized finance, allowing players to earn cryptocurrency and trade in-game assets.'),
                ('Social-Fi', 'Social-Fi integrates social media with decentralized finance, enabling monetization and decentralized governance of social platforms.'),
                ('Interoperability', 'Interoperability focuses on enabling different blockchain networks to communicate and interact, facilitating seamless asset transfers and data exchange.'),
                ('Layer 1s', 'Layer 1s are the base layer blockchains like Bitcoin and Ethereum that provide the foundational security and consensus mechanisms.'),
                ('Layer 2s', 'Layer 2s are scaling solutions built on top of Layer 1 blockchains to improve transaction speed and reduce fees.'),
                ('LSD', 'LSD (Liquid Staking Derivatives) allow users to stake assets and receive liquid tokens that can be used in DeFi activities.'),
                ('Meme', 'Meme coins are cryptocurrencies inspired by internet memes, often characterized by high volatility and community-driven value.'),
                ('NFT', 'NFTs (Non-Fungible Tokens) are unique digital assets representing ownership of items like art, music, and virtual real estate.'),
                ('Privacy', 'Privacy coins and technologies aim to enhance transaction anonymity and data protection on the blockchain.'),
                ('Real Yield', 'Real Yield focuses on generating sustainable returns through staking, lending, and other DeFi activities with real-world asset backing.'),
                ('RWA', 'RWA (Real World Assets) are physical assets like real estate or commodities tokenized on the blockchain for easier trading and investment.'),
                ('CEX', 'CEX (Centralized Exchange) refers to traditional cryptocurrency exchanges where trades are managed by a central entity.'),
                ('Stablecoins', 'Stablecoins are cryptocurrencies pegged to stable assets like fiat currencies to minimize price volatility.'),
                ('Others', 'Narrative for coins that you do not want to assign a specific Narrative.')");
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
