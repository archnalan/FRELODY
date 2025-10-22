using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class PlayList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongBooks_SongCollections_CollectionId",
                table: "SongBooks");

            migrationBuilder.DropForeignKey(
                name: "FK_SongCollections_Tenants_TenantId",
                table: "SongCollections");

            migrationBuilder.DropForeignKey(
                name: "FK_SongUserPlaylists_SongCollections_PlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.DropForeignKey(
                name: "FK_SongUserPlaylists_SongCollections_SongPlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SongCollections",
                table: "SongCollections");

            migrationBuilder.RenameTable(
                name: "SongCollections",
                newName: "Playlists");

            migrationBuilder.RenameIndex(
                name: "IX_SongCollections_TenantId",
                table: "Playlists",
                newName: "IX_Playlists_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_SongCollections_ModifiedBy",
                table: "Playlists",
                newName: "IX_Playlists_ModifiedBy");

            migrationBuilder.RenameIndex(
                name: "IX_SongCollections_IsDeleted",
                table: "Playlists",
                newName: "IX_Playlists_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_SongCollections_DateModified",
                table: "Playlists",
                newName: "IX_Playlists_DateModified");

            migrationBuilder.RenameIndex(
                name: "IX_SongCollections_DateCreated",
                table: "Playlists",
                newName: "IX_Playlists_DateCreated");

            migrationBuilder.RenameIndex(
                name: "IX_SongCollections_Access",
                table: "Playlists",
                newName: "IX_Playlists_Access");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Playlists",
                table: "Playlists",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Playlists_Tenants_TenantId",
                table: "Playlists",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SongBooks_Playlists_CollectionId",
                table: "SongBooks",
                column: "CollectionId",
                principalTable: "Playlists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SongUserPlaylists_Playlists_PlaylistId",
                table: "SongUserPlaylists",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SongUserPlaylists_Playlists_SongPlaylistId",
                table: "SongUserPlaylists",
                column: "SongPlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Playlists_Tenants_TenantId",
                table: "Playlists");

            migrationBuilder.DropForeignKey(
                name: "FK_SongBooks_Playlists_CollectionId",
                table: "SongBooks");

            migrationBuilder.DropForeignKey(
                name: "FK_SongUserPlaylists_Playlists_PlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.DropForeignKey(
                name: "FK_SongUserPlaylists_Playlists_SongPlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Playlists",
                table: "Playlists");

            migrationBuilder.RenameTable(
                name: "Playlists",
                newName: "SongCollections");

            migrationBuilder.RenameIndex(
                name: "IX_Playlists_TenantId",
                table: "SongCollections",
                newName: "IX_SongCollections_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Playlists_ModifiedBy",
                table: "SongCollections",
                newName: "IX_SongCollections_ModifiedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Playlists_IsDeleted",
                table: "SongCollections",
                newName: "IX_SongCollections_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_Playlists_DateModified",
                table: "SongCollections",
                newName: "IX_SongCollections_DateModified");

            migrationBuilder.RenameIndex(
                name: "IX_Playlists_DateCreated",
                table: "SongCollections",
                newName: "IX_SongCollections_DateCreated");

            migrationBuilder.RenameIndex(
                name: "IX_Playlists_Access",
                table: "SongCollections",
                newName: "IX_SongCollections_Access");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SongCollections",
                table: "SongCollections",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SongBooks_SongCollections_CollectionId",
                table: "SongBooks",
                column: "CollectionId",
                principalTable: "SongCollections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SongCollections_Tenants_TenantId",
                table: "SongCollections",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SongUserPlaylists_SongCollections_PlaylistId",
                table: "SongUserPlaylists",
                column: "PlaylistId",
                principalTable: "SongCollections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SongUserPlaylists_SongCollections_SongPlaylistId",
                table: "SongUserPlaylists",
                column: "SongPlaylistId",
                principalTable: "SongCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
