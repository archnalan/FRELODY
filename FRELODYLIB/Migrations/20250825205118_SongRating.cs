using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SongRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "Songs",
                type: "decimal(3,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Songs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SongUserRatings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    RevisionAtRating = table.Column<int>(type: "int", nullable: false),
                    ModificationCount = table.Column<int>(type: "int", nullable: false),
                    RatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongUserRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongUserRatings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SongUserRatings_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongUserRatings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_DateCreated",
                table: "SongUserRatings",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_DateModified",
                table: "SongUserRatings",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_IsDeleted",
                table: "SongUserRatings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_ModifiedBy",
                table: "SongUserRatings",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_SongId_UserId_TenantId",
                table: "SongUserRatings",
                columns: new[] { "SongId", "UserId", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_TenantId",
                table: "SongUserRatings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_UserId",
                table: "SongUserRatings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SongUserRatings");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Songs");
        }
    }
}
