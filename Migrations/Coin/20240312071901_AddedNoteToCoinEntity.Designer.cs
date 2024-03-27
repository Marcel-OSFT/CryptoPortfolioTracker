﻿// <auto-generated />
using System;
using CryptoPortfolioTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CryptoPortfolioTracker.Migrations.Coin
{
    [DbContext(typeof(CoinContext))]
    [Migration("20240312071901_AddedNoteToCoinEntity")]
    partial class AddedNoteToCoinEntity
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.15");

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("About")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Account");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Asset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AccountId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("AverageCostPrice")
                        .HasColumnType("REAL");

                    b.Property<int?>("CoinId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Qty")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("CoinId");

                    b.ToTable("Asset");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Coin", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("About")
                        .HasColumnType("TEXT");

                    b.Property<string>("ApiId")
                        .HasColumnType("TEXT");

                    b.Property<double>("Ath")
                        .HasColumnType("REAL");

                    b.Property<double>("Change1Month")
                        .HasColumnType("REAL");

                    b.Property<double>("Change24Hr")
                        .HasColumnType("REAL");

                    b.Property<double>("Change52Week")
                        .HasColumnType("REAL");

                    b.Property<string>("ImageUri")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsAsset")
                        .HasColumnType("INTEGER");

                    b.Property<double>("MarketCap")
                        .HasColumnType("REAL");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Note")
                        .HasColumnType("TEXT");

                    b.Property<double>("Price")
                        .HasColumnType("REAL");

                    b.Property<long>("Rank")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Symbol")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Coins");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Mutation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AssetId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Direction")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Price")
                        .HasColumnType("REAL");

                    b.Property<double>("Qty")
                        .HasColumnType("REAL");

                    b.Property<int?>("TransactionId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AssetId");

                    b.HasIndex("TransactionId");

                    b.ToTable("Mutation");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Transaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Note")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Transaction");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Asset", b =>
                {
                    b.HasOne("CryptoPortfolioTracker.Models.Account", "Account")
                        .WithMany("Assets")
                        .HasForeignKey("AccountId");

                    b.HasOne("CryptoPortfolioTracker.Models.Coin", "Coin")
                        .WithMany("Assets")
                        .HasForeignKey("CoinId");

                    b.Navigation("Account");

                    b.Navigation("Coin");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Mutation", b =>
                {
                    b.HasOne("CryptoPortfolioTracker.Models.Asset", "Asset")
                        .WithMany("Mutations")
                        .HasForeignKey("AssetId");

                    b.HasOne("CryptoPortfolioTracker.Models.Transaction", "Transaction")
                        .WithMany("Mutations")
                        .HasForeignKey("TransactionId");

                    b.Navigation("Asset");

                    b.Navigation("Transaction");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Account", b =>
                {
                    b.Navigation("Assets");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Asset", b =>
                {
                    b.Navigation("Mutations");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Coin", b =>
                {
                    b.Navigation("Assets");
                });

            modelBuilder.Entity("CryptoPortfolioTracker.Models.Transaction", b =>
                {
                    b.Navigation("Mutations");
                });
#pragma warning restore 612, 618
        }
    }
}
