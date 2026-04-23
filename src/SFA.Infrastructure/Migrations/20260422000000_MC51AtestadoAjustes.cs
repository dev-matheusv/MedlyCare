using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MC51AtestadoAjustes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona CRM ao usuário (preenchido pelo cadastro do profissional)
            migrationBuilder.AddColumn<string>(
                name: "crm",
                table: "usuario",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Adiciona suporte a horário (atestado por horas, ex.: 2h de consulta)
            migrationBuilder.AddColumn<TimeSpan>(
                name: "hora_inicio",
                table: "atestado",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "hora_fim",
                table: "atestado",
                type: "time",
                nullable: true);

            // CRM no atestado passa a ser nullable (preenchido automaticamente do usuário, que pode não ter CRM)
            migrationBuilder.AlterColumn<string>(
                name: "crm",
                table: "atestado",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "hora_inicio", table: "atestado");
            migrationBuilder.DropColumn(name: "hora_fim", table: "atestado");
            migrationBuilder.DropColumn(name: "crm", table: "usuario");

            migrationBuilder.AlterColumn<string>(
                name: "crm",
                table: "atestado",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
