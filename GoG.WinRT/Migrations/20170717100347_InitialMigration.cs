using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GoG.WinRT.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoMove",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Color = table.Column<int>(nullable: false),
                    MoveType = table.Column<int>(nullable: false),
                    Position = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoMove", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoMoveResult",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CapturedStones = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    WinMargin = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoMoveResult", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoPlayer",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Komi = table.Column<decimal>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    PlayerType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoPlayer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoGame",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    BlackPositions = table.Column<string>(nullable: true),
                    Operation = table.Column<int>(nullable: false),
                    Player1Id = table.Column<int>(nullable: true),
                    Player2Id = table.Column<int>(nullable: true),
                    ShowingArea = table.Column<bool>(nullable: false),
                    Size = table.Column<byte>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    WhitePositions = table.Column<string>(nullable: true),
                    WhoseTurn = table.Column<int>(nullable: false),
                    WinMargin = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoGame", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoGame_GoPlayer_Player1Id",
                        column: x => x.Player1Id,
                        principalTable: "GoPlayer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoGame_GoPlayer_Player2Id",
                        column: x => x.Player2Id,
                        principalTable: "GoPlayer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoMoveHistoryItem",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GoGameId = table.Column<Guid>(nullable: true),
                    MoveId = table.Column<int>(nullable: true),
                    ResultId = table.Column<int>(nullable: true),
                    Sequence = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoMoveHistoryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoMoveHistoryItem_GoGame_GoGameId",
                        column: x => x.GoGameId,
                        principalTable: "GoGame",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoMoveHistoryItem_GoMove_MoveId",
                        column: x => x.MoveId,
                        principalTable: "GoMove",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoMoveHistoryItem_GoMoveResult_ResultId",
                        column: x => x.ResultId,
                        principalTable: "GoMoveResult",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoGame_Player1Id",
                table: "GoGame",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_GoGame_Player2Id",
                table: "GoGame",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_GoMoveHistoryItem_GoGameId",
                table: "GoMoveHistoryItem",
                column: "GoGameId");

            migrationBuilder.CreateIndex(
                name: "IX_GoMoveHistoryItem_MoveId",
                table: "GoMoveHistoryItem",
                column: "MoveId");

            migrationBuilder.CreateIndex(
                name: "IX_GoMoveHistoryItem_ResultId",
                table: "GoMoveHistoryItem",
                column: "ResultId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoMoveHistoryItem");

            migrationBuilder.DropTable(
                name: "GoGame");

            migrationBuilder.DropTable(
                name: "GoMove");

            migrationBuilder.DropTable(
                name: "GoMoveResult");

            migrationBuilder.DropTable(
                name: "GoPlayer");
        }
    }
}
