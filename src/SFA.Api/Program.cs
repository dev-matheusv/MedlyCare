using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SFA.Api.Auth;
using SFA.Api.Endpoints;
using SFA.Application.Auth;
using SFA.Infrastructure;
using FluentValidation;
using SFA.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Serilog básico
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// DbContext
builder.Services.AddDbContext<SfaDbContext>(o =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    o.UseSnakeCaseNamingConvention();
});

// Config do cors pro front
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowFrontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials());
});

// 1) Bind options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

// 2) JWT bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o =>
  {
    o.TokenValidationParameters = new()
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer = jwt.Issuer,
      ValidAudience = jwt.Audience,
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
      ClockSkew = TimeSpan.Zero
    };
  });

// 3) Registrar o serviço concreto para a interface da Application
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthorization(o =>
{
  o.AddPolicy("Admin", p => p.RequireRole("Admin"));
  o.AddPolicy("Profissional", p => p.RequireRole("Profissional"));
  o.AddPolicy("Recepcao", p => p.RequireRole("Recepcao"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "SFA API", Version = "v1" });

  c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,          // <= importante
    Scheme = "bearer",                       // <= importante (minúsculo)
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Insira o token JWT no formato: Bearer {seu_token}"
  });

  c.AddSecurityRequirement(new OpenApiSecurityRequirement
  {
    {
      new OpenApiSecurityScheme
      {
        Reference = new OpenApiReference
        {
          Type = ReferenceType.SecurityScheme,
          Id = "Bearer"
        }
      },
      Array.Empty<string>()
    }
  });
});

builder.Services.AddValidatorsFromAssembly(
  typeof(SFA.Application.Empresas.EmpresaCreateValidator).Assembly
);

var app = builder.Build();

// Migrar banco automaticamente em dev (opcional)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SfaDbContext>();
    db.Database.Migrate();
    
    await DbInitializer.SeedAsync(db);
}

app.UseErrorHandling();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (ctx, next) =>
{
  var cid = ctx.Request.Headers.TryGetValue("X-Correlation-ID", out var v) && !string.IsNullOrWhiteSpace(v)
    ? v.ToString()
    : Guid.NewGuid().ToString("n");
  ctx.Response.Headers["X-Correlation-ID"] = cid;

  // Enriquecer Serilog para este escopo
  using (Serilog.Context.LogContext.PushProperty("CorrelationId", cid))
  {
    // Se autenticado, puxe claims
    var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
              ?? ctx.User.FindFirst("sub")?.Value;
    var emp = ctx.User.FindFirst("cod_empresa")?.Value;

    using (Serilog.Context.LogContext.PushProperty("UserId", uid ?? "-"))
    using (Serilog.Context.LogContext.PushProperty("EmpresaId", emp ?? "-"))
    {
      await next();
    }
  }
});

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/health/db", async (SfaDbContext db) =>
{
  var canConnect = await db.Database.CanConnectAsync();
  return canConnect 
    ? Results.Ok(new { status = "ok", db = "connected" })
    : Results.Problem("Database unavailable");
});

app.MapGet("/api/v1/empresas/segredo", () => Results.Ok("acesso admin"))
  .RequireAuthorization("Admin");

app.MapGet("/api/v1/admin/ping", () => "ok").RequireAuthorization("Admin");

app.MapAuthEndpoints();
app.MapEmpresaEndpoints();
app.MapUsuarioEndpoints();
app.MapPacienteEndpoints();
app.MapPerfilEndpoints();
app.MapUsuarioPerfilEndpoints();
app.MapAgendamentoEndpoints();

app.Run();
