using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Complexidades;
using SFA.Domain.Entities;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class PacienteComplexidadeEndpoints
{
    public static void MapPacienteComplexidadeEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/pacientes/{pacienteId:guid}/complexidade")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Profissional"));

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }

        // GET /pacientes/{pacienteId}/complexidade
        // Retorna apenas o vínculo mais recente (complexidade atual do paciente)
        g.MapGet("/", async (ClaimsPrincipal u, Guid pacienteId, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var pacienteOk = await db.Pacientes
                .AnyAsync(p => p.Id == pacienteId && p.CodEmpresa == codEmp && p.Ativo);

            if (!pacienteOk)
                return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var ultima = await db.PacientesComplexidade
                .AsNoTracking()
                .Where(pc => pc.CodEmpresa == codEmp && pc.PacienteId == pacienteId)
                .OrderByDescending(pc => pc.Data)
                .Select(pc => new PacienteComplexidadeDto(
                    pc.Id,
                    pc.PacienteId,
                    pc.ComplexidadeId,
                    pc.Complexidade.Descricao,
                    pc.Complexidade.Cor,
                    pc.UsuarioId,
                    pc.AtendimentoId,
                    pc.Data))
                .FirstOrDefaultAsync();

            if (ultima is null)
                return Results.NotFound(new { message = "complexidade_nao_vinculada" });

            return Results.Ok(ultima);
        });

        // GET /pacientes/{pacienteId}/complexidade/historico
        // Retorna todo o histórico de vínculos (mais recente primeiro)
        g.MapGet("/historico", async (
            ClaimsPrincipal u,
            Guid pacienteId,
            SfaDbContext db,
            int page = 1,
            int pageSize = 20) =>
        {
            var codEmp = GetCodEmpresa(u);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var pacienteOk = await db.Pacientes
                .AnyAsync(p => p.Id == pacienteId && p.CodEmpresa == codEmp && p.Ativo);

            if (!pacienteOk)
                return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var q = db.PacientesComplexidade
                .AsNoTracking()
                .Where(pc => pc.CodEmpresa == codEmp && pc.PacienteId == pacienteId)
                .OrderByDescending(pc => pc.Data);

            var total = await q.CountAsync();

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(pc => new PacienteComplexidadeDto(
                    pc.Id,
                    pc.PacienteId,
                    pc.ComplexidadeId,
                    pc.Complexidade.Descricao,
                    pc.Complexidade.Cor,
                    pc.UsuarioId,
                    pc.AtendimentoId,
                    pc.Data))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // POST /pacientes/{pacienteId}/complexidade
        // Vincula uma complexidade ao paciente (registros anteriores ficam como histórico)
        g.MapPost("/", async (
            ClaimsPrincipal u,
            Guid pacienteId,
            PacienteComplexidadeCreateDto dto,
            IValidator<PacienteComplexidadeCreateDto> validator,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var pacienteOk = await db.Pacientes
                .AnyAsync(p => p.Id == pacienteId && p.CodEmpresa == codEmp && p.Ativo);

            if (!pacienteOk)
                return Results.NotFound(new { message = "paciente_nao_encontrado" });

            var complexidadeOk = await db.Complexidades
                .AnyAsync(c => c.Id == dto.ComplexidadeId && c.CodEmpresa == codEmp && c.Ativo);

            if (!complexidadeOk)
                return Results.NotFound(new { message = "complexidade_nao_encontrada" });

            var usuarioOk = await db.Usuarios
                .AnyAsync(usr => usr.Id == dto.UsuarioId && usr.CodEmpresa == codEmp && usr.Ativo);

            if (!usuarioOk)
                return Results.NotFound(new { message = "usuario_nao_encontrado" });

            if (dto.AtendimentoId.HasValue)
            {
                var atendimentoOk = await db.Atendimentos
                    .AnyAsync(a => a.Id == dto.AtendimentoId.Value && a.CodEmpresa == codEmp);

                if (!atendimentoOk)
                    return Results.NotFound(new { message = "atendimento_nao_encontrado" });
            }

            var entity = new PacienteComplexidade
            {
                CodEmpresa = codEmp,
                PacienteId = pacienteId,
                ComplexidadeId = dto.ComplexidadeId,
                UsuarioId = dto.UsuarioId,
                AtendimentoId = dto.AtendimentoId,
                Data = dto.Data?.ToUniversalTime() ?? DateTime.UtcNow
            };

            db.PacientesComplexidade.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/v1/pacientes/{pacienteId}/complexidade/historico",
                new { entity.Id });
        });
    }
}
