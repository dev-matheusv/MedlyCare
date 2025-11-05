using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SFA.Application.Pacientes;
using SFA.Infrastructure;
using System.Security.Claims;

namespace SFA.Api.Endpoints;

public static class PacienteEndpoints
{
    public static void MapPacienteEndpoints(this IEndpointRouteBuilder app)
    {
        // Perfis com acesso: Profissional, Recepcao, Admin
        var g = app.MapGroup("/api/v1/pacientes")
                   .RequireAuthorization(policy => policy.RequireRole("Profissional", "Recepcao", "Admin"));

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");
            return codEmp;
        }
        
        static Guid? GetUserId(ClaimsPrincipal user)
        {
          var s = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
          return Guid.TryParse(s, out var id) ? id : null;   // antes: int
        }

        // GET /pacientes?search=&ativo=&page=1&pageSize=20&order=nome|-nome|criadoem|-criadoem
        g.MapGet("/", async (ClaimsPrincipal u, string? search, bool? ativo,
          SfaDbContext db, int page = 1, int pageSize = 20, string? order = null) =>
        {
            var codEmp = GetCodEmpresa(u);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Pacientes.AsNoTracking().Where(x => x.CodEmpresa == codEmp);

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(x =>
                    EF.Functions.ILike(x.Nome, $"%{search}%") ||
                    EF.Functions.ILike(x.Documento, $"%{search}%"));
            }

            if (ativo.HasValue)
                q = q.Where(x => x.Ativo == ativo);

            q = order?.ToLower() switch
            {
                "nome"       => q.OrderBy(x => x.Nome),
                "-nome"      => q.OrderByDescending(x => x.Nome),
                "criadoem"   => q.OrderBy(x => x.CriadoEm),
                "-criadoem"  => q.OrderByDescending(x => x.CriadoEm),
                _            => q.OrderBy(x => x.Id)
            };

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new PacienteListItemDto(x.Id, x.Nome, x.Documento, x.Ativo, x.CriadoEm, x.Telefone, x.Email, x.DataNascimento))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (ClaimsPrincipal u, Guid id, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var p = await db.Pacientes.AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp)
                .Select(x => new PacienteListItemDto(x.Id, x.Nome, x.Documento, x.Ativo, x.CriadoEm, x.Telefone, x.Email, x.DataNascimento))
                .FirstOrDefaultAsync();

            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        g.MapPost("/", async (ClaimsPrincipal u, PacienteCreateDto dto, IValidator<PacienteCreateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = new Domain.Entities.Paciente
            {
                CodEmpresa = codEmp,
                Nome = dto.Nome,
                Documento = dto.Documento,
                DataNascimento = dto.DataNascimento,
                Telefone = dto.Telefone,
                Email = dto.Email,
                Ativo = dto.Ativo
            };

            db.Pacientes.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/pacientes/{entity.Id}", new { entity.Id });
        });

        g.MapPut("/{id:guid}", async (ClaimsPrincipal u, Guid id, PacienteUpdateDto dto, IValidator<PacienteUpdateDto> v, SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(u);

            var val = await v.ValidateAsync(dto);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var entity = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
            if (entity is null) return Results.NotFound();

            entity.Nome = dto.Nome;
            entity.Documento = dto.Documento;
            entity.DataNascimento = dto.DataNascimento;
            entity.Telefone = dto.Telefone;
            entity.Email = dto.Email;
            entity.Ativo = dto.Ativo;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapDelete("/{id:guid}", async (ClaimsPrincipal u, Guid id, string? motivo, SfaDbContext db) =>
        {
          var codEmp = GetCodEmpresa(u);
          if (string.IsNullOrWhiteSpace(motivo))
            return Results.BadRequest(new { message = "motivo_obrigatorio" });

          var entity = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == id && x.CodEmpresa == codEmp);
          if (entity is null) return Results.NotFound();

          entity.IsDeleted = true;
          entity.DeletedAt = DateTimeOffset.UtcNow;
          var uid = GetUserId(u);
          if (uid.HasValue) entity.DeletedBy = uid.Value;
          entity.DeletedReason = motivo;

          await db.SaveChangesAsync();
          return Results.NoContent();
        });
    }
}
