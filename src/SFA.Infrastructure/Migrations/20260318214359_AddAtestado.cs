using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAtestado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "atestado",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atendimento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_emissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dias_afastamento = table.Column<int>(type: "integer", nullable: false),
                    data_inicio_afastamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tipo_afastamento = table.Column<int>(type: "integer", nullable: true),
                    descricao_curta = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    informar_cid = table.Column<bool>(type: "boolean", nullable: false),
                    cid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    local_emissao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    crm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assinatura_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cancelado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atestado", x => x.id);
                    table.ForeignKey(
                        name: "fk_atestado_atendimento_atendimento_id",
                        column: x => x.atendimento_id,
                        principalTable: "atendimento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atestado_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "paciente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_atestado_usuarios_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_atestado_atendimento_id",
                table: "atestado",
                column: "atendimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_atestado_cod_empresa_cancelado",
                table: "atestado",
                columns: new[] { "cod_empresa", "cancelado" });

            migrationBuilder.CreateIndex(
                name: "ix_atestado_cod_empresa_paciente_id_data_emissao",
                table: "atestado",
                columns: new[] { "cod_empresa", "paciente_id", "data_emissao" });

            migrationBuilder.CreateIndex(
                name: "ix_atestado_cod_empresa_profissional_id_data_emissao",
                table: "atestado",
                columns: new[] { "cod_empresa", "profissional_id", "data_emissao" });

            migrationBuilder.CreateIndex(
                name: "ix_atestado_paciente_id",
                table: "atestado",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_atestado_profissional_id",
                table: "atestado",
                column: "profissional_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "atestado");
        }
    }
}
