using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingExpiresAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BillingExpiresAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingExpiresAt",
                table: "AspNetUsers");
        }
    }
}
