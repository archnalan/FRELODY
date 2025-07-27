using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SongCollectionAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Songs",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectionId",
                table: "SongBooks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SongCollections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Curator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CollectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongCollections_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongBooks_CollectionId",
                table: "SongBooks",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SongCollections_DateCreated",
                table: "SongCollections",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SongCollections_DateModified",
                table: "SongCollections",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SongCollections_IsDeleted",
                table: "SongCollections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SongCollections_ModifiedBy",
                table: "SongCollections",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SongCollections_TenantId",
                table: "SongCollections",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_SongBooks_SongCollections_CollectionId",
                table: "SongBooks",
                column: "CollectionId",
                principalTable: "SongCollections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongBooks_SongCollections_CollectionId",
                table: "SongBooks");

            migrationBuilder.DropTable(
                name: "SongCollections");

            migrationBuilder.DropIndex(
                name: "IX_SongBooks_CollectionId",
                table: "SongBooks");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "SongBooks");
        }
    }
}
