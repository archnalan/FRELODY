using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class PlaylistsShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaylistId",
                table: "ShareLinks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_PlaylistId",
                table: "ShareLinks",
                column: "PlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShareLinks_Playlists_PlaylistId",
                table: "ShareLinks",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Playlists_PlaylistId",
                table: "ShareLinks");

            migrationBuilder.DropIndex(
                name: "IX_ShareLinks_PlaylistId",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "ShareLinks");
        }
    }
}
