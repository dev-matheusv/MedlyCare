using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPacienteAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anexo_paciente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_documento = table.Column<int>(type: "integer", nullable: false),
                    nome_arquivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    nome_armazenado = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                    hash_sha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    url_storage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_documento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    enviado_por_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anexo_paciente", x => x.id);
                    table.ForeignKey(
                        name: "fk_anexo_paciente_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "paciente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_anexo_paciente_usuarios_enviado_por_id",
                        column: x => x.enviado_por_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "log_acesso_anexo_paciente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    anexo_paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acao = table.Column<int>(type: "integer", nullable: false),
                    ip = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_acesso_anexo_paciente", x => x.id);
                    table.ForeignKey(
                        name: "fk_log_acesso_anexo_paciente_anexo_paciente_anexo_paciente_id",
                        column: x => x.anexo_paciente_id,
                        principalTable: "anexo_paciente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_log_acesso_anexo_paciente_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_anexo_paciente_cod_empresa_is_deleted",
                table: "anexo_paciente",
                columns: new[] { "cod_empresa", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_anexo_paciente_cod_empresa_paciente_id",
                table: "anexo_paciente",
                columns: new[] { "cod_empresa", "paciente_id" });

            migrationBuilder.CreateIndex(
                name: "ix_anexo_paciente_cod_empresa_tipo_documento",
                table: "anexo_paciente",
                columns: new[] { "cod_empresa", "tipo_documento" });

            migrationBuilder.CreateIndex(
                name: "ix_anexo_paciente_enviado_por_id",
                table: "anexo_paciente",
                column: "enviado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_anexo_paciente_hash_sha256",
                table: "anexo_paciente",
                column: "hash_sha256");

            migrationBuilder.CreateIndex(
                name: "ix_anexo_paciente_paciente_id",
                table: "anexo_paciente",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_log_acesso_anexo_paciente_anexo_paciente_id",
                table: "log_acesso_anexo_paciente",
                column: "anexo_paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_log_acesso_anexo_paciente_cod_empresa_anexo_paciente_id_dat",
                table: "log_acesso_anexo_paciente",
                columns: new[] { "cod_empresa", "anexo_paciente_id", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "ix_log_acesso_anexo_paciente_cod_empresa_usuario_id_data_hora",
                table: "log_acesso_anexo_paciente",
                columns: new[] { "cod_empresa", "usuario_id", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "ix_log_acesso_anexo_paciente_usuario_id",
                table: "log_acesso_anexo_paciente",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_acesso_anexo_paciente");

            migrationBuilder.DropTable(
                name: "anexo_paciente");
        }
    }
}
