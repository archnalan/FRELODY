using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class PlaylistSongBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SongUserCollections");

            migrationBuilder.RenameColumn(
                name: "CollectionDate",
                table: "SongCollections",
                newName: "PlaylistDate");

            migrationBuilder.CreateTable(
                name: "ArtistAlbumSongs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrackNumber = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    Access = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
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
                    SongBookId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SongNumber = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    Access = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "SongUserPlaylists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongPlaylistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    DateScheduled = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PlaylistId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    Access = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongUserPlaylists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongUserPlaylists_SongCollections_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "SongCollections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SongUserPlaylists_SongCollections_SongPlaylistId",
                        column: x => x.SongPlaylistId,
                        principalTable: "SongCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongUserPlaylists_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongUserPlaylists_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_Access",
                table: "SongUserPlaylists",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_AddedByUserId",
                table: "SongUserPlaylists",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_DateCreated",
                table: "SongUserPlaylists",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_DateModified",
                table: "SongUserPlaylists",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_DateScheduled",
                table: "SongUserPlaylists",
                column: "DateScheduled");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_IsDeleted",
                table: "SongUserPlaylists",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_ModifiedBy",
                table: "SongUserPlaylists",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserPlaylists_PlaylistId",
                table: "SongUserPlaylists",
                column: "PlaylistId");

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
                name: "IX_SongUserPlaylists_TenantId",
                table: "SongUserPlaylists",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistAlbumSongs");

            migrationBuilder.DropTable(
                name: "BookCategorySongs");

            migrationBuilder.DropTable(
                name: "SongUserPlaylists");

            migrationBuilder.RenameColumn(
                name: "PlaylistDate",
                table: "SongCollections",
                newName: "CollectionDate");

            migrationBuilder.CreateTable(
                name: "SongUserCollections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongCollectionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Access = table.Column<int>(type: "int", nullable: true),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateScheduled = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SongCollectionId1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongUserCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongUserCollections_SongCollections_SongCollectionId",
                        column: x => x.SongCollectionId,
                        principalTable: "SongCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongUserCollections_SongCollections_SongCollectionId1",
                        column: x => x.SongCollectionId1,
                        principalTable: "SongCollections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SongUserCollections_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongUserCollections_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_Access",
                table: "SongUserCollections",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_AddedByUserId",
                table: "SongUserCollections",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_DateCreated",
                table: "SongUserCollections",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_DateModified",
                table: "SongUserCollections",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_DateScheduled",
                table: "SongUserCollections",
                column: "DateScheduled");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_IsDeleted",
                table: "SongUserCollections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_ModifiedBy",
                table: "SongUserCollections",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_SongCollectionId",
                table: "SongUserCollections",
                column: "SongCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_SongCollectionId1",
                table: "SongUserCollections",
                column: "SongCollectionId1");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_SongId_SongCollectionId_TenantId",
                table: "SongUserCollections",
                columns: new[] { "SongId", "SongCollectionId", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_TenantId",
                table: "SongUserCollections",
                column: "TenantId");
        }
    }
}
