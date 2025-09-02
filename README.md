# SFA Backend (.NET 8 + PostgreSQL)

Backend do Sistema SFA (MVP), em .NET 8, PostgreSQL e Clean Architecture (API, Application, Domain, Infrastructure).

## Requisitos
- .NET 8 SDK
- Docker + Docker Compose
- EF Core CLI (opcional): `dotnet tool install --global dotnet-ef`

## Rodando em dev
1. Suba o banco:
   ```bash
   docker compose up -d
   ```
2. Atualize o `appsettings.Development.json` se necessário (connection string).
3. Restaure e rode a API:
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project src/SFA.Api/SFA.Api.csproj
   ```
4. Acesse Swagger: http://localhost:5041/swagger

## Migrations (criar a base)
```bash
dotnet ef migrations add InitialCreate -p src/SFA.Infrastructure/SFA.Infrastructure.csproj -s src/SFA.Api/SFA.Api.csproj
dotnet ef database update -p src/SFA.Infrastructure/SFA.Infrastructure.csproj -s src/SFA.Api/SFA.Api.csproj
```

## Estrutura
- `src/SFA.Api` – Host ASP.NET, Swagger, JWT, Serilog, OTel
- `src/SFA.Application` – Casos de uso, DTOs, validações, MediatR
- `src/SFA.Domain` – Entidades e regras
- `src/SFA.Infrastructure` – EF Core (DbContext), repositórios, migrations

## Banco (PostgreSQL)
- Contêiner: `postgres:16-alpine` -> `localhost:5432`
- DB: `sfa_db`, usuário: `sfa`, senha: `sfa`
- Scripts em `scripts/sql` (extensões e auditoria)

## Próximos passos
- Implementar Auth (pgcrypto + JWT)
- Criar migrations e tabelas base (Empresa, Usuario, Perfil, etc.)
- Adicionar policies e RLS (opcional) no PostgreSQL
