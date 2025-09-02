using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SFA.Infrastructure;
using Serilog;
using SFA.Api.Endpoints;

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

// Auth (JWT - placeholder, configurado mais tarde)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SFA API", Version = "v1" });
});

var app = builder.Build();

// Migrar banco automaticamente em dev (opcional)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SfaDbContext>();
    db.Database.Migrate();
    
    await DbInitializer.SeedAsync(db);
}

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

// Health
app.MapGet("/health/db", async (SfaDbContext db) =>
{
  var canConnect = await db.Database.CanConnectAsync();
  return canConnect 
    ? Results.Ok(new { status = "ok", db = "connected" })
    : Results.Problem("Database unavailable");
});

app.MapAuthEndpoints();

app.Run();
