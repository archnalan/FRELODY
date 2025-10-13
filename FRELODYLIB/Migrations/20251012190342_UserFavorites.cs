using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class UserFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Songs");

            migrationBuilder.CreateTable(
                name: "UserSongFavorites",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FavoritedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_UserSongFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSongFavorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSongFavorites_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSongFavorites_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_Access",
                table: "UserSongFavorites",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_DateCreated",
                table: "UserSongFavorites",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_DateModified",
                table: "UserSongFavorites",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_FavoritedAt",
                table: "UserSongFavorites",
                column: "FavoritedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_IsDeleted",
                table: "UserSongFavorites",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_ModifiedBy",
                table: "UserSongFavorites",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_SongId_UserId",
                table: "UserSongFavorites",
                columns: new[] { "SongId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_SongId_UserId_TenantId",
                table: "UserSongFavorites",
                columns: new[] { "SongId", "UserId", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_TenantId",
                table: "UserSongFavorites",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSongFavorites_UserId",
                table: "UserSongFavorites",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSongFavorites");

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Songs",
                type: "bit",
                nullable: true);
        }
    }
}
