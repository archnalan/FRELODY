using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class PlaylistsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongUserPlaylists_Playlists_PlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.DropForeignKey(
                name: "FK_SongUserPlaylists_Playlists_SongPlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.DropTable(
                name: "ArtistAlbumSongs");

            migrationBuilder.DropTable(
                name: "BookCategorySongs");

            migrationBuilder.DropIndex(
                name: "IX_SongUserPlaylists_SongId_SongPlaylistId_TenantId",
                table: "SongUserPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_SongUserPlaylists_SongPlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.DropColumn(
                name: "SongPlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.AlterColumn<string>(
                name: "PlaylistId",
                table: "SongUserPlaylists",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaylistId1",
                table: "SongUserPlaylists",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_PlaylistId1",
                table: "SongUserPlaylists",
                column: "PlaylistId1");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_SongId_PlaylistId_TenantId",
                table: "SongUserPlaylists",
                columns: new[] { "SongId", "PlaylistId", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SongUserPlaylists_Playlists_PlaylistId",
                table: "SongUserPlaylists",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SongUserPlaylists_Playlists_PlaylistId1",
                table: "SongUserPlaylists",
                column: "PlaylistId1",
                principalTable: "Playlists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongUserPlaylists_Playlists_PlaylistId",
                table: "SongUserPlaylists");

            migrationBuilder.DropForeignKey(
                name: "FK_SongUserPlaylists_Playlists_PlaylistId1",
                table: "SongUserPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_SongUserPlaylists_PlaylistId1",
                table: "SongUserPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_SongUserPlaylists_SongId_PlaylistId_TenantId",
                table: "SongUserPlaylists");

            migrationBuilder.DropColumn(
                name: "PlaylistId1",
                table: "SongUserPlaylists");

            migrationBuilder.AlterColumn<string>(
                name: "PlaylistId",
                table: "SongUserPlaylists",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "SongPlaylistId",
                table: "SongUserPlaylists",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ArtistAlbumSongs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Access = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TrackNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistAlbumSongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtistAlbumSongs_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArtistAlbumSongs_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtistAlbumSongs_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtistAlbumSongs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookCategorySongs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SongBookId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Access = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SongNumber = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCategorySongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookCategorySongs_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookCategorySongs_SongBooks_SongBookId",
                        column: x => x.SongBookId,
                        principalTable: "SongBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookCategorySongs_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookCategorySongs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_SongId_SongPlaylistId_TenantId",
                table: "SongUserPlaylists",
                columns: new[] { "SongId", "SongPlaylistId", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_SongPlaylistId",
                table: "SongUserPlaylists",
                column: "SongPlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_Access",
                table: "ArtistAlbumSongs",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_AlbumId",
                table: "ArtistAlbumSongs",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_ArtistId_SongId_AlbumId_TenantId",
                table: "ArtistAlbumSongs",
                columns: new[] { "ArtistId", "SongId", "AlbumId", "TenantId" },
                unique: true,
                filter: "[AlbumId] IS NOT NULL AND [TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_DateCreated",
                table: "ArtistAlbumSongs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_DateModified",
                table: "ArtistAlbumSongs",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_IsDeleted",
                table: "ArtistAlbumSongs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_ModifiedBy",
                table: "ArtistAlbumSongs",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_SongId",
                table: "ArtistAlbumSongs",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_TenantId",
                table: "ArtistAlbumSongs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_Access",
                table: "BookCategorySongs",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_CategoryId",
                table: "BookCategorySongs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_DateCreated",
                table: "BookCategorySongs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_DateModified",
                table: "BookCategorySongs",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_IsDeleted",
                table: "BookCategorySongs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_ModifiedBy",
                table: "BookCategorySongs",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_SongBookId_SongId_CategoryId_TenantId",
                table: "BookCategorySongs",
                columns: new[] { "SongBookId", "SongId", "CategoryId", "TenantId" },
                unique: true,
                filter: "[CategoryId] IS NOT NULL AND [TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_SongId",
                table: "BookCategorySongs",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategorySongs_TenantId",
                table: "BookCategorySongs",
                column: "TenantId");

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
    }
}
