using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAtendimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "atendimento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agendamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    inicio_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    finalizado_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    alterado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alterado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atendimento", x => x.id);
                    table.ForeignKey(
                        name: "fk_atendimento_agendamento_agendamento_id",
                        column: x => x.agendamento_id,
                        principalTable: "agendamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atendimento_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "paciente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atendimento_usuarios_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_atendimento_agendamento_id",
                table: "atendimento",
                column: "agendamento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_atendimento_cod_empresa_agendamento_id",
                table: "atendimento",
                columns: new[] { "cod_empresa", "agendamento_id" },
                unique: true,
                filter: "\"agendamento_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_atendimento_cod_empresa_paciente_id_inicio_utc",
                table: "atendimento",
                columns: new[] { "cod_empresa", "paciente_id", "inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_atendimento_cod_empresa_profissional_id_inicio_utc",
                table: "atendimento",
                columns: new[] { "cod_empresa", "profissional_id", "inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_atendimento_paciente_id",
                table: "atendimento",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_atendimento_profissional_id",
                table: "atendimento",
                column: "profissional_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "atendimento");
        }
    }
}
