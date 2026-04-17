using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class ShareLinkOgSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OgDescription",
                table: "ShareLinks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgHtml",
                table: "ShareLinks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgImagePath",
                table: "ShareLinks",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgTitle",
                table: "ShareLinks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OgDescription",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "OgHtml",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "OgImagePath",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "OgTitle",
                table: "ShareLinks");
        }
    }
}
