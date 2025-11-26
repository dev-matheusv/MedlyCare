using System.Text;
using Amazon;
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
using Npgsql;
using SFA.Api.Middlewares;
using Amazon.RDS.Util;
using Amazon.Runtime.Credentials;

var builder = WebApplication.CreateBuilder(args);

// Serilog básico
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

string BuildConnectionString(IConfiguration cfg)
{
  var baseCs = cfg.GetConnectionString("Default") ?? "";
  var csb = new NpgsqlConnectionStringBuilder(baseCs);

  var host = cfg["DB_HOST"];
  var name = cfg["DB_NAME"];
  var user = cfg["DB_USER"];
  var pass = cfg["DB_PASSWORD"];
  var portStr = cfg["DB_PORT"];

  if (!string.IsNullOrWhiteSpace(host)) csb.Host = host;
  if (!string.IsNullOrWhiteSpace(name)) csb.Database = name;
  if (!string.IsNullOrWhiteSpace(user)) csb.Username = user;
  if (!string.IsNullOrWhiteSpace(pass)) csb.Password = pass;
  if (int.TryParse(portStr, out var port) && port > 0) csb.Port = port;

  // RDS geralmente exige SSL
  if(!builder.Environment.IsDevelopment()){
    csb.SslMode = SslMode.Require;
  }
  

  Log.Information("DB: Host={Host} Port={Port} Db={Db} User={User} (merge appsettings + env)",
    csb.Host, csb.Port, csb.Database, csb.Username);

  return csb.ToString();
}

var useIam = string.Equals(
  builder.Configuration["DB_AUTH_MODE"],
  "IAM",
  StringComparison.OrdinalIgnoreCase
);

builder.Services.AddDbContext<SfaDbContext>(o =>
{
  var connString = BuildConnectionString(builder.Configuration);

  if (useIam)
  {
    // AWS Region como RegionEndpoint
    var regionString =
      builder.Configuration["AWS_REGION"] ??
      Environment.GetEnvironmentVariable("AWS_REGION") ??
      "sa-east-1";

    var region = RegionEndpoint.GetBySystemName(regionString);

    // Credenciais da task role (ECS)
    var credentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

    var dsb = new NpgsqlDataSourceBuilder(connString);

    // Npgsql 8.0.3 => provider recebe (csb, ct)
    dsb.UsePeriodicPasswordProvider(
      (csb, _) =>
      {
        var token = RDSAuthTokenGenerator.GenerateAuthToken(
          credentials,     // credenciais IAM
          region,          // RegionEndpoint
          csb.Host,        // host do banco
          csb.Port,        // porta
          csb.Username     // usuário
        );

        return new ValueTask<string>(token);
      },
      TimeSpan.FromMinutes(5), // refresh OK
      TimeSpan.FromMinutes(1)  // refresh em caso de erro
    );

    var dataSource = dsb.Build();
    o.UseNpgsql(dataSource);
  }
  else
  {
    o.UseNpgsql(connString);
  }

  o.UseSnakeCaseNamingConvention();
});

// Config do cors pro front
// Lê AllowedOrigins tanto como array (Cors:AllowedOrigins:0,1,2) quanto como CSV (Cors:AllowedOrigins)
string[]? allowedArray = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
var allowedCsv = builder.Configuration["Cors:AllowedOrigins"];

string[] allowed =
  allowedArray is { Length: > 0 }
    ? allowedArray
    : (allowedCsv?.Split(',', ';', ' ',
      (char)(StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) ?? Array.Empty<string>());

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowFrontend", policy =>
  {
    if (allowed.Length > 0)
    {
      policy.WithOrigins(allowed)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // só quando há origens explícitas
    }
    else
    {
      // fallback seguro: libera sem credenciais
      policy.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
      // NÃO usar AllowCredentials com AllowAnyOrigin
    }
  });
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
if (app.Environment.IsStaging() || app.Environment.IsDevelopment())
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
