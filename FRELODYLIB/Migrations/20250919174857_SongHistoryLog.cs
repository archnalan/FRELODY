using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SongHistoryLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserFeedback",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Tenants",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SongUserRatings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Songs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SongParts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SongCollections",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SongBooks",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Settings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Pages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LyricSegments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LyricLines",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Chords",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ChordCharts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Categories",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SongPlayHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlaySource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SongId1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserId1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                    table.PrimaryKey("PK_SongPlayHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongPlayHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongPlayHistories_AspNetUsers_UserId1",
                        column: x => x.UserId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SongPlayHistories_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongPlayHistories_Songs_SongId1",
                        column: x => x.SongId1,
                        principalTable: "Songs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SongPlayHistories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_Access",
                table: "SongPlayHistories",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_DateCreated",
                table: "SongPlayHistories",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_DateModified",
                table: "SongPlayHistories",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_IsDeleted",
                table: "SongPlayHistories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_ModifiedBy",
                table: "SongPlayHistories",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_PlayedAt",
                table: "SongPlayHistories",
                column: "PlayedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_PlaySource",
                table: "SongPlayHistories",
                column: "PlaySource");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_SongId_UserId",
                table: "SongPlayHistories",
                columns: new[] { "SongId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_SongId1",
                table: "SongPlayHistories",
                column: "SongId1");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_TenantId",
                table: "SongPlayHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_UserId",
                table: "SongPlayHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlayHistories_UserId1",
                table: "SongPlayHistories",
                column: "UserId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SongPlayHistories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserFeedback");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SongUserRatings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SongParts");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SongCollections");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SongBooks");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LyricSegments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LyricLines");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Chords");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ChordCharts");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetUsers");
        }
    }
}
