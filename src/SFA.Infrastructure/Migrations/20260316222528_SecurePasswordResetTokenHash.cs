using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SecurePasswordResetTokenHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token",
                table: "password_reset_token",
                newName: "token_hash");

            migrationBuilder.RenameIndex(
                name: "ix_password_reset_token_token",
                table: "password_reset_token",
                newName: "ix_password_reset_token_token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "password_reset_token",
                newName: "token");

            migrationBuilder.RenameIndex(
                name: "ix_password_reset_token_token_hash",
                table: "password_reset_token",
                newName: "ix_password_reset_token_token");
        }
    }
}
