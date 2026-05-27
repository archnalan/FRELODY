using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class AddTikTokDiscover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TikTokVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Uploader = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TikTokVideos", x => x.Id);
                    table.UniqueConstraint("AK_TikTokVideos_VideoId", x => x.VideoId);
                });

            migrationBuilder.CreateTable(
                name: "TikTokTranscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BeatModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChordModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChordDict = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BeatsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChordsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SyncedChordsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bpm = table.Column<float>(type: "real", nullable: true),
                    TimeSignature = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    KeySignature = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalProcessingSeconds = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TikTokTranscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TikTokTranscriptions_TikTokVideos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "TikTokVideos",
                        principalColumn: "VideoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TikTokTranscriptions_VideoId_BeatModel_ChordModel_ChordDict",
                table: "TikTokTranscriptions",
                columns: new[] { "VideoId", "BeatModel", "ChordModel", "ChordDict" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TikTokVideos_VideoId",
                table: "TikTokVideos",
                column: "VideoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TikTokTranscriptions");

            migrationBuilder.DropTable(
                name: "TikTokVideos");
        }
    }
}
