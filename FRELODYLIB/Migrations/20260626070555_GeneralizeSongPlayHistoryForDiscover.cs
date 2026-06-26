using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeSongPlayHistoryForDiscover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SongId",
                table: "SongPlayHistories",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "MediaTitle",
                table: "SongPlayHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "SongPlayHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "SongPlayHistories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "SongPlayHistories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoId",
                table: "SongPlayHistories",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_Platform_VideoId",
                table: "SongPlayHistories",
                columns: new[] { "Platform", "VideoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SongPlayHistories_Platform_VideoId",
                table: "SongPlayHistories");

            migrationBuilder.DropColumn(
                name: "MediaTitle",
                table: "SongPlayHistories");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "SongPlayHistories");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "SongPlayHistories");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "SongPlayHistories");

            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "SongPlayHistories");

            migrationBuilder.AlterColumn<string>(
                name: "SongId",
                table: "SongPlayHistories",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
