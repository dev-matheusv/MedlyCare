using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agendamento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    paciente_id = table.Column<int>(type: "integer", nullable: false),
                    profissional_id = table.Column<int>(type: "integer", nullable: false),
                    inicio_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fim_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "agendado"),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    criado_por_usuario_id = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    alterado_por_usuario_id = table.Column<int>(type: "integer", nullable: true),
                    alterado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agendamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_agendamento_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "paciente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agendamento_usuarios_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_cod_empresa_paciente_id_inicio_utc",
                table: "agendamento",
                columns: new[] { "cod_empresa", "paciente_id", "inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_cod_empresa_profissional_id_inicio_utc",
                table: "agendamento",
                columns: new[] { "cod_empresa", "profissional_id", "inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_paciente_id",
                table: "agendamento",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_profissional_id",
                table: "agendamento",
                column: "profissional_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agendamento");
        }
    }
}
