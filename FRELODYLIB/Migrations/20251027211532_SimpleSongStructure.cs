using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SimpleSongStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Albums_AlbumId",
                table: "Songs");

            migrationBuilder.DropTable(
                name: "ArtistAlbumSongs");

            migrationBuilder.DropTable(
                name: "BookCategorySongs");

            migrationBuilder.RenameColumn(
                name: "DisplayNumber",
                table: "Songs",
                newName: "SongNumber");

            migrationBuilder.AddColumn<string>(
                name: "ArtistId",
                table: "Songs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SongBookId",
                table: "Songs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ArtistId",
                table: "Songs",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_SongBookId",
                table: "Songs",
                column: "SongBookId");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_SongNumber",
                table: "Songs",
                column: "SongNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_Title_Slug",
                table: "Songs",
                columns: new[] { "Title", "Slug" });

            migrationBuilder.CreateIndex(
                name: "IX_Songs_WrittenBy",
                table: "Songs",
                column: "WrittenBy");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_WrittenDateRange",
                table: "Songs",
                column: "WrittenDateRange");

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_Albums_AlbumId",
                table: "Songs",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_Artists_ArtistId",
                table: "Songs",
                column: "ArtistId",
                principalTable: "Artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_SongBooks_SongBookId",
                table: "Songs",
                column: "SongBookId",
                principalTable: "SongBooks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Albums_AlbumId",
                table: "Songs");

            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Artists_ArtistId",
                table: "Songs");

            migrationBuilder.DropForeignKey(
                name: "FK_Songs_SongBooks_SongBookId",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_ArtistId",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_SongBookId",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_SongNumber",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_Title_Slug",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_WrittenBy",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_WrittenDateRange",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "ArtistId",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "SongBookId",
                table: "Songs");

            migrationBuilder.RenameColumn(
                name: "SongNumber",
                table: "Songs",
                newName: "DisplayNumber");

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
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_ArtistAlbumSongs_Access",
                table: "ArtistAlbumSongs",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_AlbumId",
                table: "ArtistAlbumSongs",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistAlbumSongs_ArtistId_AlbumId_SongId",
                table: "ArtistAlbumSongs",
                columns: new[] { "ArtistId", "AlbumId", "SongId" },
                unique: true,
                filter: "[AlbumId] IS NOT NULL");

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
                name: "IX_BookCategorySongs_SongBookId_CategoryId_SongId",
                table: "BookCategorySongs",
                columns: new[] { "SongBookId", "CategoryId", "SongId" },
                unique: true,
                filter: "[CategoryId] IS NOT NULL");

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
                name: "FK_Songs_Albums_AlbumId",
                table: "Songs",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id");
        }
    }
}
