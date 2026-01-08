using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfirmacaoAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "confirmacao_agendamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    agendamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    respondido_em_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    respondido_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_confirmacao_agendamento", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_confirmacao_agendamento_agendamento_id",
                table: "confirmacao_agendamento",
                column: "agendamento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_confirmacao_agendamento_cod_empresa_status_expires_at_utc",
                table: "confirmacao_agendamento",
                columns: new[] { "cod_empresa", "status", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_confirmacao_agendamento_token",
                table: "confirmacao_agendamento",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "confirmacao_agendamento");
        }
    }
}
