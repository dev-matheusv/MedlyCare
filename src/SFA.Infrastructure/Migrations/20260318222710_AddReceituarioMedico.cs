using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceituarioMedico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "receituario_medico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atendimento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_emissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancelado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receituario_medico", x => x.id);
                    table.ForeignKey(
                        name: "fk_receituario_medico_atendimento_atendimento_id",
                        column: x => x.atendimento_id,
                        principalTable: "atendimento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receituario_medico_paciente_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "paciente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receituario_medico_usuarios_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receituario_medico_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    receituario_medico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_medicamento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    forma_farmaceutica = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    concentracao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    via_administracao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    posologia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    orientacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receituario_medico_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_receituario_medico_item_receituario_medico_receituario_medi",
                        column: x => x.receituario_medico_id,
                        principalTable: "receituario_medico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_receituario_medico_atendimento_id",
                table: "receituario_medico",
                column: "atendimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_receituario_medico_cod_empresa_cancelado",
                table: "receituario_medico",
                columns: new[] { "cod_empresa", "cancelado" });

            migrationBuilder.CreateIndex(
                name: "ix_receituario_medico_cod_empresa_paciente_id_data_emissao",
                table: "receituario_medico",
                columns: new[] { "cod_empresa", "paciente_id", "data_emissao" });

            migrationBuilder.CreateIndex(
                name: "ix_receituario_medico_cod_empresa_profissional_id_data_emissao",
                table: "receituario_medico",
                columns: new[] { "cod_empresa", "profissional_id", "data_emissao" });

            migrationBuilder.CreateIndex(
                name: "ix_receituario_medico_paciente_id",
                table: "receituario_medico",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_receituario_medico_profissional_id",
                table: "receituario_medico",
                column: "profissional_id");

            migrationBuilder.CreateIndex(
                name: "ix_receituario_medico_item_receituario_medico_id",
                table: "receituario_medico_item",
                column: "receituario_medico_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receituario_medico_item");

            migrationBuilder.DropTable(
                name: "receituario_medico");
        }
    }
}
