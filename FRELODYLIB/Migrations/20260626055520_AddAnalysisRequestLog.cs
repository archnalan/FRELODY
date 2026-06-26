using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisRequestLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisRequestLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VideoId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WasPremium = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChannelTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    FirstRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_AnalysisRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisRequestLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalyzedVideoWhitelists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VideoId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedByEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
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
                    table.PrimaryKey("PK_AnalyzedVideoWhitelists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyzedVideoWhitelists_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_Access",
                table: "AnalysisRequestLogs",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_DateCreated",
                table: "AnalysisRequestLogs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_DateModified",
                table: "AnalysisRequestLogs",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_IsDeleted",
                table: "AnalysisRequestLogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_ModifiedBy",
                table: "AnalysisRequestLogs",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_Platform_VideoId",
                table: "AnalysisRequestLogs",
                columns: new[] { "Platform", "VideoId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_Platform_VideoId_UserId_RequestDate",
                table: "AnalysisRequestLogs",
                columns: new[] { "Platform", "VideoId", "UserId", "RequestDate" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_RequestDate",
                table: "AnalysisRequestLogs",
                column: "RequestDate");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRequestLogs_TenantId",
                table: "AnalysisRequestLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedVideoWhitelists_Access",
                table: "AnalyzedVideoWhitelists",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedVideoWhitelists_DateCreated",
                table: "AnalyzedVideoWhitelists",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedVideoWhitelists_DateModified",
                table: "AnalyzedVideoWhitelists",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedVideoWhitelists_IsDeleted",
                table: "AnalyzedVideoWhitelists",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedVideoWhitelists_ModifiedBy",
                table: "AnalyzedVideoWhitelists",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedVideoWhitelists_Platform_VideoId",
                table: "AnalyzedVideoWhitelists",
                columns: new[] { "Platform", "VideoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedVideoWhitelists_TenantId",
                table: "AnalyzedVideoWhitelists",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisRequestLogs");

            migrationBuilder.DropTable(
                name: "AnalyzedVideoWhitelists");
        }
    }
}
