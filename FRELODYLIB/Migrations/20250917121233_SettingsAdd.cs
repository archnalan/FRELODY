using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SettingsAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChordFont = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LyricFont = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SongDisplay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChordDisplay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChordDifficulty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlayLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    Access = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Access",
                table: "Settings",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_DateCreated",
                table: "Settings",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_DateModified",
                table: "Settings",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_IsDeleted",
                table: "Settings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_ModifiedBy",
                table: "Settings",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_TenantId",
                table: "Settings",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
