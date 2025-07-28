using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class SimplifiedSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LyricLines_Bridges_BridgeId",
                table: "LyricLines");

            migrationBuilder.DropForeignKey(
                name: "FK_LyricLines_Choruses_ChorusId",
                table: "LyricLines");

            migrationBuilder.DropForeignKey(
                name: "FK_LyricLines_Verses_VerseId",
                table: "LyricLines");

            migrationBuilder.DropTable(
                name: "Bridges");

            migrationBuilder.DropTable(
                name: "Choruses");

            migrationBuilder.DropTable(
                name: "Verses");

            migrationBuilder.DropIndex(
                name: "IX_LyricLines_BridgeId",
                table: "LyricLines");

            migrationBuilder.DropIndex(
                name: "IX_LyricLines_ChorusId",
                table: "LyricLines");

            migrationBuilder.DropColumn(
                name: "BridgeId",
                table: "LyricLines");

            migrationBuilder.DropColumn(
                name: "ChorusId",
                table: "LyricLines");

            migrationBuilder.DropColumn(
                name: "PartName",
                table: "LyricLines");

            migrationBuilder.RenameColumn(
                name: "VerseId",
                table: "LyricLines",
                newName: "PartId");

            migrationBuilder.RenameIndex(
                name: "IX_LyricLines_VerseId",
                table: "LyricLines",
                newName: "IX_LyricLines_PartId");

            migrationBuilder.CreateTable(
                name: "SongParts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PartNumber = table.Column<int>(type: "int", nullable: false),
                    PartName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RepeatCount = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongParts_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongParts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongParts_DateCreated",
                table: "SongParts",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SongParts_DateModified",
                table: "SongParts",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_SongParts_IsDeleted",
                table: "SongParts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SongParts_ModifiedBy",
                table: "SongParts",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SongParts_SongId",
                table: "SongParts",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_SongParts_TenantId",
                table: "SongParts",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_LyricLines_SongParts_PartId",
                table: "LyricLines",
                column: "PartId",
                principalTable: "SongParts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LyricLines_SongParts_PartId",
                table: "LyricLines");

            migrationBuilder.DropTable(
                name: "SongParts");

            migrationBuilder.RenameColumn(
                name: "PartId",
                table: "LyricLines",
                newName: "VerseId");

            migrationBuilder.RenameIndex(
                name: "IX_LyricLines_PartId",
                table: "LyricLines",
                newName: "IX_LyricLines_VerseId");

            migrationBuilder.AddColumn<string>(
                name: "BridgeId",
                table: "LyricLines",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChorusId",
                table: "LyricLines",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartName",
                table: "LyricLines",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Bridges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BridgeNumber = table.Column<int>(type: "int", nullable: true),
                    BridgeTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RepeatCount = table.Column<int>(type: "int", nullable: true),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bridges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bridges_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bridges_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Choruses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChorusNumber = table.Column<int>(type: "int", nullable: true),
                    ChorusTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RepeatCount = table.Column<int>(type: "int", nullable: true),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Choruses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Choruses_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Choruses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Verses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RepeatCount = table.Column<int>(type: "int", nullable: true),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VerseNumber = table.Column<int>(type: "int", nullable: false),
                    VerseTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Verses_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Verses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LyricLines_BridgeId",
                table: "LyricLines",
                column: "BridgeId");

            migrationBuilder.CreateIndex(
                name: "IX_LyricLines_ChorusId",
                table: "LyricLines",
                column: "ChorusId");

            migrationBuilder.CreateIndex(
                name: "IX_Bridges_DateCreated",
                table: "Bridges",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Bridges_DateModified",
                table: "Bridges",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_Bridges_IsDeleted",
                table: "Bridges",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Bridges_ModifiedBy",
                table: "Bridges",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Bridges_SongId",
                table: "Bridges",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Bridges_TenantId",
                table: "Bridges",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Choruses_DateCreated",
                table: "Choruses",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Choruses_DateModified",
                table: "Choruses",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_Choruses_IsDeleted",
                table: "Choruses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Choruses_ModifiedBy",
                table: "Choruses",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Choruses_SongId",
                table: "Choruses",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Choruses_TenantId",
                table: "Choruses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_DateCreated",
                table: "Verses",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_DateModified",
                table: "Verses",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_IsDeleted",
                table: "Verses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_ModifiedBy",
                table: "Verses",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_SongId",
                table: "Verses",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_TenantId",
                table: "Verses",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_LyricLines_Bridges_BridgeId",
                table: "LyricLines",
                column: "BridgeId",
                principalTable: "Bridges",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LyricLines_Choruses_ChorusId",
                table: "LyricLines",
                column: "ChorusId",
                principalTable: "Choruses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LyricLines_Verses_VerseId",
                table: "LyricLines",
                column: "VerseId",
                principalTable: "Verses",
                principalColumn: "Id");
        }
    }
}
