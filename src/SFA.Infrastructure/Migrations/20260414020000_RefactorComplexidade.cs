using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorComplexidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove tabela antiga (modelo errado)
            migrationBuilder.DropTable(name: "complexidade_paciente");

            // Tabela mestre: complexidade (cadastro configurável por empresa)
            migrationBuilder.CreateTable(
                name: "complexidade",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complexidade", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_complexidade_cod_empresa_ativo",
                table: "complexidade",
                columns: new[] { "cod_empresa", "ativo" });

            // Tabela de vínculo: paciente_complexidade (histórico)
            migrationBuilder.CreateTable(
                name: "paciente_complexidade",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    complexidade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atendimento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paciente_complexidade", x => x.id);

                    table.ForeignKey(
                        name: "fk_paciente_complexidade_paciente",
                        column: x => x.paciente_id,
                        principalTable: "paciente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "fk_paciente_complexidade_complexidade",
                        column: x => x.complexidade_id,
                        principalTable: "complexidade",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "fk_paciente_complexidade_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "fk_paciente_complexidade_atendimento",
                        column: x => x.atendimento_id,
                        principalTable: "atendimento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_paciente_complexidade_cod_empresa_paciente_id_data",
                table: "paciente_complexidade",
                columns: new[] { "cod_empresa", "paciente_id", "data" });

            migrationBuilder.CreateIndex(
                name: "ix_paciente_complexidade_cod_empresa_complexidade_id",
                table: "paciente_complexidade",
                columns: new[] { "cod_empresa", "complexidade_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "paciente_complexidade");
            migrationBuilder.DropTable(name: "complexidade");

            // Recria tabela antiga (rollback)
            migrationBuilder.CreateTable(
                name: "complexidade_paciente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nivel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    criado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complexidade_paciente", x => x.id);
                });
        }
    }
}
