using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "nome",
                table: "empresa",
                newName: "razao_social");

            migrationBuilder.RenameIndex(
                name: "ix_empresa_nome",
                table: "empresa",
                newName: "ix_empresa_razao_social");

            migrationBuilder.AddColumn<string>(
                name: "bairro",
                table: "empresa",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "celular_whatsapp",
                table: "empresa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cep",
                table: "empresa",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cidade",
                table: "empresa",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cnae",
                table: "empresa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "documento",
                table: "empresa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "empresa",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "endereco",
                table: "empresa",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "inscricao_estadual_rg",
                table: "empresa",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "numero_imovel",
                table: "empresa",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "pais",
                table: "empresa",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "path_logotipo",
                table: "empresa",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "redes_sociais",
                table: "empresa",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "responsavel_clinica",
                table: "empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "telefone",
                table: "empresa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uf",
                table: "empresa",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "utilizar_celular_para_envio_mensagens",
                table: "empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_empresa_documento",
                table: "empresa",
                column: "documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_empresa_email",
                table: "empresa",
                column: "email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_empresa_documento",
                table: "empresa");

            migrationBuilder.DropIndex(
                name: "ix_empresa_email",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "bairro",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "celular_whatsapp",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "cep",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "cidade",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "cnae",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "documento",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "email",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "endereco",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "inscricao_estadual_rg",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "numero_imovel",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "pais",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "path_logotipo",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "redes_sociais",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "responsavel_clinica",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "telefone",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "uf",
                table: "empresa");

            migrationBuilder.DropColumn(
                name: "utilizar_celular_para_envio_mensagens",
                table: "empresa");

            migrationBuilder.RenameColumn(
                name: "razao_social",
                table: "empresa",
                newName: "nome");

            migrationBuilder.RenameIndex(
                name: "ix_empresa_razao_social",
                table: "empresa",
                newName: "ix_empresa_nome");
        }
    }
}
