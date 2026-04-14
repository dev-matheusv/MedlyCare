using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReceituarioMedico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── receituario_medico: novos campos ──────────────────────────────

            migrationBuilder.AddColumn<int>(
                name: "tipo_receituario",
                table: "receituario_medico",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "diagnostico",
                table: "receituario_medico",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "informar_cid",
                table: "receituario_medico",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "cid",
                table: "receituario_medico",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "assinatura_nome",
                table: "receituario_medico",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "registro_profissional",
                table: "receituario_medico",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "endereco_profissional",
                table: "receituario_medico",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            // Ampliar observacoes de 1000 para 2000
            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "receituario_medico",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            // ── receituario_medico_item: novos campos ─────────────────────────

            migrationBuilder.AddColumn<string>(
                name: "quantidade",
                table: "receituario_medico_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "quantidade_extenso",
                table: "receituario_medico_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // Ampliar campos existentes conforme especificação
            migrationBuilder.AlterColumn<string>(
                name: "nome_medicamento",
                table: "receituario_medico_item",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "posologia",
                table: "receituario_medico_item",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "orientacoes",
                table: "receituario_medico_item",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "tipo_receituario",       table: "receituario_medico");
            migrationBuilder.DropColumn(name: "diagnostico",            table: "receituario_medico");
            migrationBuilder.DropColumn(name: "informar_cid",           table: "receituario_medico");
            migrationBuilder.DropColumn(name: "cid",                    table: "receituario_medico");
            migrationBuilder.DropColumn(name: "assinatura_nome",        table: "receituario_medico");
            migrationBuilder.DropColumn(name: "registro_profissional",  table: "receituario_medico");
            migrationBuilder.DropColumn(name: "endereco_profissional",  table: "receituario_medico");

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "receituario_medico",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.DropColumn(name: "quantidade",         table: "receituario_medico_item");
            migrationBuilder.DropColumn(name: "quantidade_extenso", table: "receituario_medico_item");

            migrationBuilder.AlterColumn<string>(
                name: "nome_medicamento",
                table: "receituario_medico_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "posologia",
                table: "receituario_medico_item",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "orientacoes",
                table: "receituario_medico_item",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
