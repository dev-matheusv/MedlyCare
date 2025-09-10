# 🏥 SFA Backend – Sistema de Clínicas

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat&logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

> Backend do **Sistema SFA (Sistema de Clínicas)**, reescrito em **.NET 8** com **PostgreSQL**, utilizando **Clean Architecture** e suporte **multi-clínicas**.

---

## 📌 Visão Geral

Este projeto é a nova versão do **SFA Backend**, originalmente em Delphi, agora modernizado em **.NET 8** e **Vue.js** (frontend separado).  
Focado em **segurança, escalabilidade e boas práticas**, traz:

- Multi-empresa (**clínicas**) com isolamento de dados.  
- **Autenticação segura**: `pgcrypto` (bcrypt no PostgreSQL) + JWT.  
- RBAC: controle de acesso baseado em perfis (admin, profissional, recepção).  
- Gestão de **usuários, pacientes, atendimentos e status**.  
- **Auditoria automática** em todas as tabelas críticas (old/new em JSON).  
- Observabilidade: **Serilog + OpenTelemetry**.  
- Deploy e dev com **Docker Compose**.

> ℹ️ Nota: Este repositório está aberto temporariamente para portfólio.  
> O projeto segue em evolução e poderá ser privado futuramente.

---

## 🛠️ Tecnologias

- [ASP.NET Core 8](https://learn.microsoft.com/aspnet/core) – API REST
- [Entity Framework Core 8](https://learn.microsoft.com/ef/core) – ORM
- [PostgreSQL 16](https://www.postgresql.org/) – Banco relacional
- [pgcrypto](https://www.postgresql.org/docs/current/pgcrypto.html) – Hash de senhas
- [Serilog](https://serilog.net/) – Logging estruturado
- [OpenTelemetry](https://opentelemetry.io/) – Tracing e métricas
- [Docker Compose](https://docs.docker.com/compose/) – Infra local (Postgres + pgAdmin)
- [MediatR](https://github.com/jbogard/MediatR) – CQRS leve
- [FluentValidation](https://fluentvalidation.net/) – Validações

---

## ⚙️ Setup do Projeto

### 1. Subir banco de dados
```bash
docker compose up -d
```

### 2. Rodar aplicação
```bash
dotnet restore
dotnet build
dotnet run --project src/SFA.Api/SFA.Api.csproj
```

Acesse o **Swagger** em: [http://localhost:5041/swagger](http://localhost:5041/swagger)

### 3. Migrations (primeira vez)
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p src/SFA.Infrastructure/SFA.Infrastructure.csproj -s src/SFA.Api/SFA.Api.csproj
dotnet ef database update -p src/SFA.Infrastructure/SFA.Infrastructure.csproj -s src/SFA.Api/SFA.Api.csproj
```

---

## 📂 Estrutura da Solução

```
src/
 ├── SFA.Api/              → ASP.NET Core (Swagger, JWT, Serilog)
 ├── SFA.Application/      → Casos de uso, validações, CQRS (MediatR)
 ├── SFA.Domain/           → Entidades, agregados e enums
 └── SFA.Infrastructure/   → EF Core, DbContext, migrations, repositórios

scripts/sql/               → Scripts do PostgreSQL (extensões, auditoria)
docker-compose.yml         → Postgres + pgAdmin
```

---

## 🔒 Segurança

- **Autenticação**: login + senha (bcrypt via `pgcrypto`).  
- **Autorização**: RBAC baseado em perfis.  
- **Auditoria**: triggers automáticas salvam operações (INSERT/UPDATE/DELETE) com usuário, empresa, txid, valores old/new.  
- **Multi-empresa**: `cod_empresa` em todas as tabelas + suporte a **Row-Level Security (RLS)**.

---

## 📊 Roadmap Backend (MVP)

- ✅ **Semana 1**: Setup, Docker, Migrations iniciais  
- ✅ **Semana 2**: Auth (pgcrypto + JWT + RBAC)  
- 🚧 **Semana 3**: CRUD Empresas/Perfis  
- 🚧 **Semana 4**: CRUD Pacientes + Anexos  
- 🚧 **Semana 5**: Atendimentos e Status  
- 🚧 **Semana 6**: Relatórios (sintético/detalhado)  
- 🚧 **Semana 7**: Auditoria completa (JSON old/new)  
- 🚧 **Semana 8**: Testes E2E, CI/CD e deploy

---

## 🤝 Contribuição

1. Faça um fork do projeto  
2. Crie uma branch para sua feature (`git checkout -b feature/minha-feature`)  
3. Commit suas mudanças (`git commit -m "feat: adicionei minha feature"`)  
4. Faça push para a branch (`git push origin feature/minha-feature`)  
5. Abra um Pull Request  

---

## 📄 Licença

Este projeto está sob licença MIT – veja o arquivo [LICENSE](LICENSE) para mais detalhes.
