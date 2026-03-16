using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioContactFieldsAndProfissionais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "celular_whatsapp",
                table: "usuario",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "usuario",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "telefone",
                table: "usuario",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_cod_empresa_email",
                table: "usuario",
                columns: new[] { "cod_empresa", "email" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_usuario_cod_empresa_email",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "celular_whatsapp",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "email",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "telefone",
                table: "usuario");
        }
    }
}
