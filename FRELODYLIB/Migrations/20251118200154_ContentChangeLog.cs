using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class ContentChangeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLoginDate",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentChangeLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ChangeTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangeDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPublicContent = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ContentChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentChangeLogs_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentChangeLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoginTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastLogoutTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActiveSession = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_UserLoginHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLoginHistories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentChangeLogs_Access",
                table: "ContentChangeLogs",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChangeLogs_ChangedByUserId_EntityId",
                table: "ContentChangeLogs",
                columns: new[] { "ChangedByUserId", "EntityId" },
                unique: true,
                filter: "[ChangedByUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChangeLogs_DateCreated",
                table: "ContentChangeLogs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChangeLogs_DateModified",
                table: "ContentChangeLogs",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChangeLogs_IsDeleted",
                table: "ContentChangeLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChangeLogs_ModifiedBy",
                table: "ContentChangeLogs",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentChangeLogs_TenantId",
                table: "ContentChangeLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_Access",
                table: "UserLoginHistories",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_DateCreated",
                table: "UserLoginHistories",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_DateModified",
                table: "UserLoginHistories",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_IsDeleted",
                table: "UserLoginHistories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_ModifiedBy",
                table: "UserLoginHistories",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_TenantId",
                table: "UserLoginHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginHistories_UserId_LoginTime",
                table: "UserLoginHistories",
                columns: new[] { "UserId", "LoginTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentChangeLogs");

            migrationBuilder.DropTable(
                name: "UserLoginHistories");

            migrationBuilder.DropColumn(
                name: "LastLoginDate",
                table: "AspNetUsers");
        }
    }
}
