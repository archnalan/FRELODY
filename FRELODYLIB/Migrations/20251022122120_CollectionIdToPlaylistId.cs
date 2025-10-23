using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class CollectionIdToPlaylistId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongBooks_Playlists_CollectionId",
                table: "SongBooks");

            migrationBuilder.RenameColumn(
                name: "CollectionId",
                table: "SongBooks",
                newName: "PlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_SongBooks_CollectionId",
                table: "SongBooks",
                newName: "IX_SongBooks_PlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_SongBooks_Playlists_PlaylistId",
                table: "SongBooks",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongBooks_Playlists_PlaylistId",
                table: "SongBooks");

            migrationBuilder.RenameColumn(
                name: "PlaylistId",
                table: "SongBooks",
                newName: "CollectionId");

            migrationBuilder.RenameIndex(
                name: "IX_SongBooks_PlaylistId",
                table: "SongBooks",
                newName: "IX_SongBooks_CollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SongBooks_Playlists_CollectionId",
                table: "SongBooks",
                column: "CollectionId",
                principalTable: "Playlists",
                principalColumn: "Id");
        }
    }
}
