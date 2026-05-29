using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyzedSongUnlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyzedSongUnlocks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VideoId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DateModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    Access = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzedSongUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyzedSongUnlocks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalyzedSongUnlocks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedSongUnlocks_Access",
                table: "AnalyzedSongUnlocks",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedSongUnlocks_DateCreated",
                table: "AnalyzedSongUnlocks",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedSongUnlocks_DateModified",
                table: "AnalyzedSongUnlocks",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedSongUnlocks_IsDeleted",
                table: "AnalyzedSongUnlocks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedSongUnlocks_ModifiedBy",
                table: "AnalyzedSongUnlocks",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedSongUnlocks_TenantId",
                table: "AnalyzedSongUnlocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedSongUnlocks_UserId_Platform_VideoId_UnlockedAt",
                table: "AnalyzedSongUnlocks",
                columns: new[] { "UserId", "Platform", "VideoId", "UnlockedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedSongUnlocks_UserId_UnlockedAt",
                table: "AnalyzedSongUnlocks",
                columns: new[] { "UserId", "UnlockedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyzedSongUnlocks");
        }
    }
}
