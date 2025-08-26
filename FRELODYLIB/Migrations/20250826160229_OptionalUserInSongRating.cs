using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class OptionalUserInSongRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SongUserRatings_SongId_UserId_TenantId",
                table: "SongUserRatings");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "SongUserRatings",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_SongId_UserId_TenantId",
                table: "SongUserRatings",
                columns: new[] { "SongId", "UserId", "TenantId" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SongUserRatings_SongId_UserId_TenantId",
                table: "SongUserRatings");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "SongUserRatings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SongUserRatings_SongId_UserId_TenantId",
                table: "SongUserRatings",
                columns: new[] { "SongId", "UserId", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
