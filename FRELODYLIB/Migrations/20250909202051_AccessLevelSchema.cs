using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class AccessLevelSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "UserFeedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "SongUserRatings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "Songs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "SongParts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "SongCollections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "SongBooks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "Pages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "LyricSegments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "LyricLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "Chords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "ChordCharts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedback_Access",
                table: "UserFeedback",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Access",
                table: "Tenants",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_Access",
                table: "SongUserRatings",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_Access",
                table: "Songs",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongParts_Access",
                table: "SongParts",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongCollections_Access",
                table: "SongCollections",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_SongBooks_Access",
                table: "SongBooks",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Access",
                table: "Pages",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_LyricSegments_Access",
                table: "LyricSegments",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_LyricLines_Access",
                table: "LyricLines",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_Chords_Access",
                table: "Chords",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_ChordCharts_Access",
                table: "ChordCharts",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Access",
                table: "Categories",
                column: "Access");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserFeedback_Access",
                table: "UserFeedback");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Access",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_SongUserRatings_Access",
                table: "SongUserRatings");

            migrationBuilder.DropIndex(
                name: "IX_Songs_Access",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_SongParts_Access",
                table: "SongParts");

            migrationBuilder.DropIndex(
                name: "IX_SongCollections_Access",
                table: "SongCollections");

            migrationBuilder.DropIndex(
                name: "IX_SongBooks_Access",
                table: "SongBooks");

            migrationBuilder.DropIndex(
                name: "IX_Pages_Access",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_LyricSegments_Access",
                table: "LyricSegments");

            migrationBuilder.DropIndex(
                name: "IX_LyricLines_Access",
                table: "LyricLines");

            migrationBuilder.DropIndex(
                name: "IX_Chords_Access",
                table: "Chords");

            migrationBuilder.DropIndex(
                name: "IX_ChordCharts_Access",
                table: "ChordCharts");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Access",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "UserFeedback");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "SongUserRatings");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "SongParts");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "SongCollections");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "SongBooks");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "LyricSegments");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "LyricLines");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "Chords");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "ChordCharts");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "Categories");
        }
    }
}
