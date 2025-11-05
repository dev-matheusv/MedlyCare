using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "empresa",
                columns: table => new
                {
                    CodEmpresa = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresa", x => x.CodEmpresa);
                });

            migrationBuilder.CreateTable(
                name: "paciente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CodEmpresa = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paciente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "perfil",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CodEmpresa = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CodEmpresa = table.Column<int>(type: "integer", nullable: false),
                    Login = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agendamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cod_empresa = table.Column<int>(type: "integer", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profissional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fim_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "agendado"),
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
                    table.PrimaryKey("PK_agendamento", x => x.id);
                    table.ForeignKey(
                        name: "FK_agendamento_paciente_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "paciente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agendamento_usuario_profissional_id",
                        column: x => x.profissional_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario_perfil",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    perfil_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_perfil", x => new { x.usuario_id, x.perfil_id });
                    table.ForeignKey(
                        name: "FK_usuario_perfil_perfil_perfil_id",
                        column: x => x.perfil_id,
                        principalTable: "perfil",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuario_perfil_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agendamento_cod_empresa_paciente_id_inicio_utc",
                table: "agendamento",
                columns: new[] { "cod_empresa", "paciente_id", "inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_agendamento_cod_empresa_profissional_id_inicio_utc",
                table: "agendamento",
                columns: new[] { "cod_empresa", "profissional_id", "inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_agendamento_paciente_id",
                table: "agendamento",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_agendamento_profissional_id",
                table: "agendamento",
                column: "profissional_id");

            migrationBuilder.CreateIndex(
                name: "IX_empresa_Nome",
                table: "empresa",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_paciente_CodEmpresa_Documento",
                table: "paciente",
                columns: new[] { "CodEmpresa", "Documento" });

            migrationBuilder.CreateIndex(
                name: "IX_paciente_CodEmpresa_Nome",
                table: "paciente",
                columns: new[] { "CodEmpresa", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_perfil_CodEmpresa_Nome",
                table: "perfil",
                columns: new[] { "CodEmpresa", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_CodEmpresa_Login",
                table: "usuario",
                columns: new[] { "CodEmpresa", "Login" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_perfil_perfil_id",
                table: "usuario_perfil",
                column: "perfil_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_perfil_usuario_id",
                table: "usuario_perfil",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agendamento");

            migrationBuilder.DropTable(
                name: "empresa");

            migrationBuilder.DropTable(
                name: "usuario_perfil");

            migrationBuilder.DropTable(
                name: "paciente");

            migrationBuilder.DropTable(
                name: "perfil");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
