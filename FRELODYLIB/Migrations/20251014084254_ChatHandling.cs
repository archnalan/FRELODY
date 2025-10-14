using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class ChatHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VisitorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VisitorEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedAdminId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChatSessionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SenderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsFromAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Access",
                table: "ChatMessages",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId",
                table: "ChatMessages",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_DateCreated",
                table: "ChatMessages",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_DateModified",
                table: "ChatMessages",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_IsDeleted",
                table: "ChatMessages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ModifiedBy",
                table: "ChatMessages",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_TenantId",
                table: "ChatMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_Access",
                table: "ChatSessions",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_DateCreated",
                table: "ChatSessions",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_DateModified",
                table: "ChatSessions",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_IsDeleted",
                table: "ChatSessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_ModifiedBy",
                table: "ChatSessions",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_TenantId",
                table: "ChatSessions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatSessions");
        }
    }
}
