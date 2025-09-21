using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FRELODYLIB.Migrations
{
    /// <inheritdoc />
    public partial class TenantChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Access",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_DateCreated",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_DateModified",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_IsDeleted",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_ModifiedBy",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Tenants");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Tenants",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Access",
                table: "Tenants",
                column: "Access");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DateCreated",
                table: "Tenants",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DateModified",
                table: "Tenants",
                column: "DateModified");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsDeleted",
                table: "Tenants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ModifiedBy",
                table: "Tenants",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants",
                column: "TenantId");
        }
    }
}
