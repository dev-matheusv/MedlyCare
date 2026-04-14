using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Complexidades;
using SFA.Domain.Entities;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class ComplexidadePacienteEndpoints
{
    public static void MapComplexidadePacienteEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/pacientes/{pacienteId:guid}/complexidade")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Recepcao", "Profissional"));

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        static Guid? GetUserId(ClaimsPrincipal user)
        {
            var s = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            return Guid.TryParse(s, out var id) ? id : null;
        }

        static bool IsAdmin(ClaimsPrincipal u)
            => u.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

        // Verifica se o profissional informado possui perfil de fisioterapia na empresa
        static async Task<bool> ProfissionalTemFisioterapia(SfaDbContext db, int codEmpresa, Guid profissionalId)
        {
            return await db.UsuariosPerfis
                .AsNoTracking()
                .Where(up => up.UsuarioId == profissionalId)
                .Join(db.Perfis.AsNoTracking(),
                    up => up.PerfilId,
                    p => p.Id,
                    (up, p) => p)
                .AnyAsync(p =>
                    p.CodEmpresa == codEmpresa &&
                    p.Ativo &&
                    p.Nome.ToLower().Contains("fisioterapia"));
        }

        // GET /pacientes/{pacienteId}/complexidade
        // Retorna apenas a última complexidade cadastrada
        g.MapGet("/", async (ClaimsPrincipal u, Guid pacienteId, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var pacienteOk = await db.Pacientes.AnyAsync(p =>
                p.Id == pacienteId && p.CodEmpresa == codEmp && p.Ativo);

            if (!pacienteOk)
                return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var ultima = await db.ComplexidadesPaciente
                .AsNoTracking()
                .Where(c => c.CodEmpresa == codEmp && c.PacienteId == pacienteId)
                .OrderByDescending(c => c.CriadoEm)
                .Select(c => new ComplexidadePacienteDto(
                    c.Id,
                    c.PacienteId,
                    c.ProfissionalId,
                    c.Nivel,
                    c.Cor,
                    c.Observacoes,
                    c.CriadoEm,
                    c.CriadoPorUsuarioId))
                .FirstOrDefaultAsync();

            if (ultima is null)
                return Results.NotFound(new { message = "complexidade_nao_encontrada" });

            return Results.Ok(ultima);
        });

        // GET /pacientes/{pacienteId}/complexidade/historico
        // Retorna todo o histórico (mais recente primeiro)
        g.MapGet("/historico", async (ClaimsPrincipal u, Guid pacienteId, SfaDbContext db,
            int page = 1, int pageSize = 20) =>
        {
            var codEmp = GetCodEmpresa(u);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var pacienteOk = await db.Pacientes.AnyAsync(p =>
                p.Id == pacienteId && p.CodEmpresa == codEmp && p.Ativo);

            if (!pacienteOk)
                return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var q = db.ComplexidadesPaciente
                .AsNoTracking()
                .Where(c => c.CodEmpresa == codEmp && c.PacienteId == pacienteId)
                .OrderByDescending(c => c.CriadoEm);

            var total = await q.CountAsync();

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ComplexidadePacienteDto(
                    c.Id,
                    c.PacienteId,
                    c.ProfissionalId,
                    c.Nivel,
                    c.Cor,
                    c.Observacoes,
                    c.CriadoEm,
                    c.CriadoPorUsuarioId))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // POST /pacientes/{pacienteId}/complexidade
        // Cria novo registro de complexidade; registros anteriores ficam como histórico
        g.MapPost("/", async (
            ClaimsPrincipal u,
            Guid pacienteId,
            ComplexidadePacienteCreateDto dto,
            IValidator<ComplexidadePacienteCreateDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var pacienteOk = await db.Pacientes.AnyAsync(p =>
                p.Id == pacienteId && p.CodEmpresa == codEmp && p.Ativo);

            if (!pacienteOk)
                return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var profissionalOk = await db.Usuarios.AnyAsync(u2 =>
                u2.Id == dto.ProfissionalId && u2.CodEmpresa == codEmp && u2.Ativo);

            if (!profissionalOk)
                return Results.NotFound(new { message = "profissional_nao_encontrado" });

            // Valida que o profissional tem perfil de fisioterapia (Admin está isento)
            if (!IsAdmin(u))
            {
                var temFisio = await ProfissionalTemFisioterapia(db, codEmp, dto.ProfissionalId);
                if (!temFisio)
                    return Results.Forbid();
            }

            var userId = GetUserId(u) ?? Guid.Empty;

            var entity = new ComplexidadePaciente
            {
                CodEmpresa = codEmp,
                PacienteId = pacienteId,
                ProfissionalId = dto.ProfissionalId,
                Nivel = dto.Nivel.Trim().ToLowerInvariant(),
                Cor = dto.Cor.Trim(),
                Observacoes = dto.Observacoes,
                CriadoPorUsuarioId = userId,
            };

            db.ComplexidadesPaciente.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/v1/pacientes/{pacienteId}/complexidade/historico",
                new { entity.Id });
        });
    }
}
