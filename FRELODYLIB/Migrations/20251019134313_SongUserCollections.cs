using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SongUserCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "UserRefreshTokens");

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "UserRefreshTokens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserRefreshTokens",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "UserRefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                table: "UserRefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserRefreshTokens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "UserRefreshTokens",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "UserRefreshTokens",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SongUserCollections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongCollectionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    DateScheduled = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SongCollectionId1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                    table.PrimaryKey("PK_SongUserCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongUserCollections_SongCollections_SongCollectionId",
                        column: x => x.SongCollectionId,
                        principalTable: "SongCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongUserCollections_SongCollections_SongCollectionId1",
                        column: x => x.SongCollectionId1,
                        principalTable: "SongCollections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SongUserCollections_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongUserCollections_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_Access",
                table: "UserRefreshTokens",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_DateCreated",
                table: "UserRefreshTokens",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_DateModified",
                table: "UserRefreshTokens",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_IsDeleted",
                table: "UserRefreshTokens",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_ModifiedBy",
                table: "UserRefreshTokens",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_TenantId",
                table: "UserRefreshTokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_Access",
                table: "SongUserCollections",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_AddedByUserId",
                table: "SongUserCollections",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_DateCreated",
                table: "SongUserCollections",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_DateModified",
                table: "SongUserCollections",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_DateScheduled",
                table: "SongUserCollections",
                column: "DateScheduled");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_IsDeleted",
                table: "SongUserCollections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_ModifiedBy",
                table: "SongUserCollections",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_SongCollectionId",
                table: "SongUserCollections",
                column: "SongCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_SongCollectionId1",
                table: "SongUserCollections",
                column: "SongCollectionId1");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_SongId_SongCollectionId_TenantId",
                table: "SongUserCollections",
                columns: new[] { "SongId", "SongCollectionId", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserCollections_TenantId",
                table: "SongUserCollections",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRefreshTokens_Tenants_TenantId",
                table: "UserRefreshTokens",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRefreshTokens_Tenants_TenantId",
                table: "UserRefreshTokens");

            migrationBuilder.DropTable(
                name: "SongUserCollections");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_Access",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_DateCreated",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_DateModified",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_IsDeleted",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_ModifiedBy",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_TenantId",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "DateModified",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UserRefreshTokens");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "UserRefreshTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
