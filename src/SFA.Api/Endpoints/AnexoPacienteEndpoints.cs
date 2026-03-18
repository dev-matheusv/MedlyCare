using System.Security.Claims;
using System.Security.Cryptography;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFA.Application.AnexosPaciente;
using SFA.Application.Storage;
using SFA.Domain.Entities;
using SFA.Domain.Enums;
using SFA.Infrastructure;

namespace SFA.Api.Endpoints;

public static class AnexoPacienteEndpoints
{
    public static void MapAnexoPacienteEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/pacientes/{pacienteId:guid}/anexos")
            .RequireAuthorization();

        static int GetCodEmpresa(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst("cod_empresa")?.Value, out var codEmp))
                throw new InvalidOperationException("cod_empresa ausente no token.");

            return codEmp;
        }

        static Guid GetUserId(ClaimsPrincipal user)
        {
            var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst("sub")?.Value;

            if (!Guid.TryParse(value, out var userId))
                throw new InvalidOperationException("user id ausente no token.");

            return userId;
        }

        static string? GetIp(HttpContext httpContext)
        {
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        static async Task<string> CalcularSha256Async(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanSeek)
                stream.Position = 0;

            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(stream, cancellationToken);

            if (stream.CanSeek)
                stream.Position = 0;

            return Convert.ToHexString(hash);
        }

        static string GerarNomeArmazenado(string nomeOriginal)
        {
          var extensao = Path.GetExtension(nomeOriginal);

          if (string.IsNullOrWhiteSpace(extensao))
            extensao = ".bin";

          return $"{Guid.NewGuid()}{extensao}".ToLowerInvariant();
        }

        static string MontarStorageKey(int codEmpresa, Guid pacienteId, string nomeArmazenado)
        {
            return $"empresas/{codEmpresa}/pacientes/{pacienteId}/anexos/{nomeArmazenado}";
        }

        static bool ContentTypePermitido(string? contentType)
        {
            var permitidos = new[]
            {
                "application/pdf",
                "image/png",
                "image/jpeg"
            };

            return permitidos.Contains(contentType?.Trim().ToLowerInvariant());
        }

        static async Task<bool> PacienteExisteAsync(SfaDbContext db, int codEmp, Guid pacienteId)
        {
            return await db.Pacientes
                .AsNoTracking()
                .AnyAsync(x => x.Id == pacienteId && x.CodEmpresa == codEmp);
        }

        static async Task RegistrarLogAsync(
            SfaDbContext db,
            int codEmp,
            Guid anexoPacienteId,
            Guid usuarioId,
            AcaoAcessoAnexoPaciente acao,
            string? ip,
            CancellationToken cancellationToken = default)
        {
            db.LogsAcessoAnexoPaciente.Add(new LogAcessoAnexoPaciente
            {
                CodEmpresa = codEmp,
                AnexoPacienteId = anexoPacienteId,
                UsuarioId = usuarioId,
                Acao = acao,
                Ip = ip,
                DataHora = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        
        static bool TamanhoArquivoPermitido(long tamanhoBytes)
        {
          const long limite = 10 * 1024 * 1024; // 10 MB
          return tamanhoBytes > 0 && tamanhoBytes <= limite;
        }

        g.MapGet("/", async (
            ClaimsPrincipal user,
            Guid pacienteId,
            SfaDbContext db,
            int page = 1,
            int pageSize = 20) =>
        {
            var codEmp = GetCodEmpresa(user);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var pacienteExiste = await PacienteExisteAsync(db, codEmp, pacienteId);
            if (!pacienteExiste)
                return Results.NotFound();

            var query = db.AnexosPaciente
                .AsNoTracking()
                .Where(x => x.CodEmpresa == codEmp && x.PacienteId == pacienteId)
                .OrderByDescending(x => x.CriadoEm);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AnexoPacienteListItemDto(
                    x.Id,
                    x.PacienteId,
                    (int)x.TipoDocumento,
                    x.TipoDocumento.ToDescricao(),
                    x.NomeArquivo,
                    x.ContentType,
                    x.TamanhoBytes,
                    x.Descricao,
                    x.DataDocumento,
                    x.EnviadoPorId,
                    x.CriadoEm
                ))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        g.MapGet("/{id:guid}", async (
            ClaimsPrincipal user,
            Guid pacienteId,
            Guid id,
            SfaDbContext db) =>
        {
            var codEmp = GetCodEmpresa(user);

            var item = await db.AnexosPaciente
                .AsNoTracking()
                .Where(x => x.Id == id && x.CodEmpresa == codEmp && x.PacienteId == pacienteId)
                .Select(x => new AnexoPacienteDetailsDto(
                    x.Id,
                    x.PacienteId,
                    x.CodEmpresa,
                    (int)x.TipoDocumento,
                    x.TipoDocumento.ToDescricao(),
                    x.NomeArquivo,
                    x.NomeArmazenado,
                    x.ContentType,
                    x.TamanhoBytes,
                    x.HashSha256,
                    x.UrlStorage,
                    x.Descricao,
                    x.DataDocumento,
                    x.EnviadoPorId,
                    x.CriadoEm,
                    x.AtualizadoEm
                ))
                .FirstOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        g.MapPost("/", async Task<IResult> (
    ClaimsPrincipal user,
    Guid pacienteId,
    IFormFile? file,
    [FromForm] int tipoDocumento,
    [FromForm] string? descricao,
    [FromForm] DateTime? dataDocumento,
    IValidator<AnexoPacienteUploadDto> validator,
    SfaDbContext db,
    IFileStorageService storage,
    CancellationToken cancellationToken) =>
{
    var codEmp = GetCodEmpresa(user);
    var usuarioId = GetUserId(user);

    var pacienteExiste = await PacienteExisteAsync(db, codEmp, pacienteId);
    if (!pacienteExiste)
        return Results.NotFound();

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new
        {
            message = "arquivo_obrigatorio"
        });
    }

    if (!TamanhoArquivoPermitido(file.Length))
    {
        return Results.BadRequest(new
        {
            message = "tamanho_arquivo_nao_permitido"
        });
    }

    if (!ContentTypePermitido(file.ContentType))
    {
        return Results.BadRequest(new
        {
            message = "content_type_nao_permitido"
        });
    }

    var dto = new AnexoPacienteUploadDto(
        tipoDocumento,
        descricao,
        dataDocumento
    );

    var validation = await validator.ValidateAsync(dto, cancellationToken);
    if (!validation.IsValid)
        return Results.ValidationProblem(validation.ToDictionary());

    await using var stream = file.OpenReadStream();

    var hashSha256 = await CalcularSha256Async(stream, cancellationToken);
    var nomeArmazenado = GerarNomeArmazenado(file.FileName);
    var storageKey = MontarStorageKey(codEmp, pacienteId, nomeArmazenado);

    try
    {
        await storage.UploadAsync(storageKey, stream, file.ContentType, cancellationToken);

        var entity = new AnexoPaciente
        {
            CodEmpresa = codEmp,
            PacienteId = pacienteId,
            TipoDocumento = (TipoDocumentoPaciente)dto.TipoDocumento,
            NomeArquivo = file.FileName,
            NomeArmazenado = nomeArmazenado,
            ContentType = file.ContentType,
            TamanhoBytes = file.Length,
            HashSha256 = hashSha256,
            UrlStorage = storageKey,
            Descricao = dto.Descricao,
            DataDocumento = dto.DataDocumento,
            EnviadoPorId = usuarioId,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow,
            IsDeleted = false
        };

        db.AnexosPaciente.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/pacientes/{pacienteId}/anexos/{entity.Id}", new
        {
            entity.Id,
            entity.PacienteId,
            entity.NomeArquivo
        });
    }
    catch
    {
        try
        {
            await storage.DeleteAsync(storageKey, cancellationToken);
        }
        catch
        {
          // ignored
        }

        throw;
    }
})
.DisableAntiforgery();

        g.MapGet("/{id:guid}/download", async (
            ClaimsPrincipal user,
            HttpContext httpContext,
            Guid pacienteId,
            Guid id,
            SfaDbContext db,
            IFileStorageService storage,
            CancellationToken cancellationToken) =>
        {
            var codEmp = GetCodEmpresa(user);
            var usuarioId = GetUserId(user);

            var entity = await db.AnexosPaciente
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.CodEmpresa == codEmp &&
                    x.PacienteId == pacienteId,
                    cancellationToken);

            if (entity is null)
                return Results.NotFound();

            var stream = await storage.DownloadAsync(entity.UrlStorage, cancellationToken);

            await RegistrarLogAsync(
                db,
                codEmp,
                entity.Id,
                usuarioId,
                AcaoAcessoAnexoPaciente.Download,
                GetIp(httpContext),
                cancellationToken);

            return Results.File(stream, entity.ContentType, entity.NomeArquivo);
        });

        g.MapGet("/{id:guid}/visualizar", async (
            ClaimsPrincipal user,
            HttpContext httpContext,
            Guid pacienteId,
            Guid id,
            SfaDbContext db,
            IFileStorageService storage,
            CancellationToken cancellationToken) =>
        {
            var codEmp = GetCodEmpresa(user);
            var usuarioId = GetUserId(user);

            var entity = await db.AnexosPaciente
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.CodEmpresa == codEmp &&
                    x.PacienteId == pacienteId,
                    cancellationToken);

            if (entity is null)
                return Results.NotFound();

            var stream = await storage.DownloadAsync(entity.UrlStorage, cancellationToken);

            await RegistrarLogAsync(
                db,
                codEmp,
                entity.Id,
                usuarioId,
                AcaoAcessoAnexoPaciente.Visualizou,
                GetIp(httpContext),
                cancellationToken);

            return Results.File(stream, entity.ContentType, enableRangeProcessing: false);
        });

        g.MapPost("/{id:guid}/excluir", async Task<IResult> (
          ClaimsPrincipal user,
          HttpContext httpContext,
          Guid pacienteId,
          Guid id,
          [FromBody] AnexoPacienteDeleteDto dto,
          IValidator<AnexoPacienteDeleteDto> validator,
          SfaDbContext db,
          CancellationToken cancellationToken) =>
        {
          var codEmp = GetCodEmpresa(user);
          var usuarioId = GetUserId(user);

          var validation = await validator.ValidateAsync(dto, cancellationToken);
          if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

          var entity = await db.AnexosPaciente
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.CodEmpresa == codEmp &&
                x.PacienteId == pacienteId,
              cancellationToken);

          if (entity is null)
            return Results.NotFound();

          entity.IsDeleted = true;
          entity.DeletedAt = DateTimeOffset.UtcNow;
          entity.DeletedBy = usuarioId;
          entity.DeletedReason = dto.Motivo;
          entity.AtualizadoEm = DateTime.UtcNow;

          await db.SaveChangesAsync(cancellationToken);

          await RegistrarLogAsync(
            db,
            codEmp,
            entity.Id,
            usuarioId,
            AcaoAcessoAnexoPaciente.Excluiu,
            GetIp(httpContext),
            cancellationToken);

          return Results.NoContent();
        });
    }
}
