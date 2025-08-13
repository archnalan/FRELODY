using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class ShareLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId1",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryId1",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId1",
                table: "Categories");

            migrationBuilder.CreateTable(
                name: "ShareLinks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ShareToken = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareLinks_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_CreatedAt",
                table: "ShareLinks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_ExpiresAt",
                table: "ShareLinks",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_ShareToken",
                table: "ShareLinks",
                column: "ShareToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_SongId",
                table: "ShareLinks",
                column: "SongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShareLinks");

            migrationBuilder.AddColumn<string>(
                name: "ParentCategoryId1",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId1",
                table: "Categories",
                column: "ParentCategoryId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId1",
                table: "Categories",
                column: "ParentCategoryId1",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
