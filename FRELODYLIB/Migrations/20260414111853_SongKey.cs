using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SongKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Transpose",
                table: "SongUserPlaylists",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Songs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Transpose",
                table: "SongUserPlaylists");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Songs");
        }
    }
}
