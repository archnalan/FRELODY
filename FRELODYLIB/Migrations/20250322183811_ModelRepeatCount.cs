using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class ModelRepeatCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepeatCount",
                table: "Verses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartName",
                table: "LyricLines",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RepeatCount",
                table: "LyricLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatCount",
                table: "Choruses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatCount",
                table: "Bridges",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepeatCount",
                table: "Verses");

            migrationBuilder.DropColumn(
                name: "PartName",
                table: "LyricLines");

            migrationBuilder.DropColumn(
                name: "RepeatCount",
                table: "LyricLines");

            migrationBuilder.DropColumn(
                name: "RepeatCount",
                table: "Choruses");

            migrationBuilder.DropColumn(
                name: "RepeatCount",
                table: "Bridges");
        }
    }
}
