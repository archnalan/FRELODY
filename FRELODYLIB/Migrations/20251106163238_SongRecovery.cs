using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SongRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SongRecoveries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecoveryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecoveryTimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_SongRecoveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongRecoveries_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongRecoveries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongRecoveries_Access",
                table: "SongRecoveries",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongRecoveries_DateCreated",
                table: "SongRecoveries",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SongRecoveries_DateModified",
                table: "SongRecoveries",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SongRecoveries_IsDeleted",
                table: "SongRecoveries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SongRecoveries_ModifiedBy",
                table: "SongRecoveries",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SongRecoveries_SongId",
                table: "SongRecoveries",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_SongRecoveries_TenantId",
                table: "SongRecoveries",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SongRecoveries");
        }
    }
}
