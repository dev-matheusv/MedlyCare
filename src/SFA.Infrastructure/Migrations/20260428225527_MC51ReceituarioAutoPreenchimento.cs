using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MC51ReceituarioAutoPreenchimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RegistroProfissional (CRM) e EnderecoProfissional passam a ser nullable.
            // Sao preenchidos automaticamente: CRM vem do cadastro do profissional logado
            // e o endereco vem da empresa (Cidade/UF). Como o CRM no usuario e nullable,
            // estes campos tambem precisam aceitar null.
            migrationBuilder.AlterColumn<string>(
                name: "registro_profissional",
                table: "receituario_medico",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "endereco_profissional",
                table: "receituario_medico",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "registro_profissional",
                table: "receituario_medico",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "endereco_profissional",
                table: "receituario_medico",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);
        }
    }
}
