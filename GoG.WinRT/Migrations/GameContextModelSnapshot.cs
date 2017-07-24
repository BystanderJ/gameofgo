using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using GoG.WinRT.Model;
using GoG.Infrastructure.Engine;

namespace GoG.WinRT.Migrations
{
    [DbContext(typeof(GameContext))]
    partial class GameContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity("GoG.Infrastructure.Engine.GoGame", b =>
                {
                    b.Property<Guid>("Id");

                    b.Property<string>("BlackPositions");

                    b.Property<DateTime>("Created");

                    b.Property<int>("Operation");

                    b.Property<int?>("Player1Id");

                    b.Property<int?>("Player2Id");

                    b.Property<bool>("ShowingArea");

                    b.Property<byte>("Size");

                    b.Property<int>("Status");

                    b.Property<string>("WhitePositions");

                    b.Property<int>("WhoseTurn");

                    b.HasKey("Id");

                    b.HasIndex("Player1Id");

                    b.HasIndex("Player2Id");

                    b.ToTable("GoGame");
                });

            modelBuilder.Entity("GoG.Infrastructure.Engine.GoMove", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Color");

                    b.Property<int>("MoveType");

                    b.Property<string>("Position");

                    b.HasKey("Id");

                    b.ToTable("GoMove");
                });

            modelBuilder.Entity("GoG.Infrastructure.Engine.GoMoveHistoryItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid?>("GoGameId");

                    b.Property<int?>("MoveId");

                    b.Property<int?>("ResultId");

                    b.Property<int>("Sequence");

                    b.HasKey("Id");

                    b.HasIndex("GoGameId");

                    b.HasIndex("MoveId");

                    b.HasIndex("ResultId");

                    b.ToTable("GoMoveHistoryItem");
                });

            modelBuilder.Entity("GoG.Infrastructure.Engine.GoMoveResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CapturedStones");

                    b.Property<int>("Status");

                    b.HasKey("Id");

                    b.ToTable("GoMoveResult");
                });

            modelBuilder.Entity("GoG.Infrastructure.Engine.GoPlayer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("Komi");

                    b.Property<int>("Level");

                    b.Property<string>("Name");

                    b.Property<int>("PlayerType");

                    b.HasKey("Id");

                    b.ToTable("GoPlayer");
                });

            modelBuilder.Entity("GoG.Infrastructure.Engine.GoGame", b =>
                {
                    b.HasOne("GoG.Infrastructure.Engine.GoPlayer", "Player1")
                        .WithMany()
                        .HasForeignKey("Player1Id");

                    b.HasOne("GoG.Infrastructure.Engine.GoPlayer", "Player2")
                        .WithMany()
                        .HasForeignKey("Player2Id");
                });

            modelBuilder.Entity("GoG.Infrastructure.Engine.GoMoveHistoryItem", b =>
                {
                    b.HasOne("GoG.Infrastructure.Engine.GoGame")
                        .WithMany("GoMoveHistory")
                        .HasForeignKey("GoGameId");

                    b.HasOne("GoG.Infrastructure.Engine.GoMove", "Move")
                        .WithMany()
                        .HasForeignKey("MoveId");

                    b.HasOne("GoG.Infrastructure.Engine.GoMoveResult", "Result")
                        .WithMany()
                        .HasForeignKey("ResultId");
                });
        }
    }
}
