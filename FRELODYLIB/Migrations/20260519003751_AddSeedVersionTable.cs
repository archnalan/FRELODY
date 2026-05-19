using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedVersionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeedVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SeedName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SeededAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_SeedVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeedVersions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeedVersions_Access",
                table: "SeedVersions",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SeedVersions_DateCreated",
                table: "SeedVersions",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SeedVersions_DateModified",
                table: "SeedVersions",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SeedVersions_IsDeleted",
                table: "SeedVersions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SeedVersions_ModifiedBy",
                table: "SeedVersions",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SeedVersions_TenantId",
                table: "SeedVersions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeedVersions");
        }
    }
}
