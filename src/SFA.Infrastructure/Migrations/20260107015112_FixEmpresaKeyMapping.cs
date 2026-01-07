using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEmpresaKeyMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_empresa_id",
                table: "empresa",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_empresa_id",
                table: "empresa");
        }
    }
}
