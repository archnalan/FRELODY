using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class AddYouTubeDiscover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YouTubeVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoId = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChannelTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YouTubeVideos", x => x.Id);
                    table.UniqueConstraint("AK_YouTubeVideos_VideoId", x => x.VideoId);
                });

            migrationBuilder.CreateTable(
                name: "YouTubeTranscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoId = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
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
                    table.PrimaryKey("PK_YouTubeTranscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YouTubeTranscriptions_YouTubeVideos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "YouTubeVideos",
                        principalColumn: "VideoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeTranscriptions_VideoId_BeatModel_ChordModel_ChordDict",
                table: "YouTubeTranscriptions",
                columns: new[] { "VideoId", "BeatModel", "ChordModel", "ChordDict" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeVideos_VideoId",
                table: "YouTubeVideos",
                column: "VideoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YouTubeTranscriptions");

            migrationBuilder.DropTable(
                name: "YouTubeVideos");
        }
    }
}
