# 🏥 MedlyCare Backend – Clinic Management System

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat\&logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat\&logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat\&logo=docker)](https://www.docker.com/)
[![AWS](https://img.shields.io/badge/AWS-ECS%20%7C%20RDS%20%7C%20ECR-FF9900?style=flat\&logo=amazonaws)](https://aws.amazon.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

> Backend of **MedlyCare**, a clinic management system built with **.NET 8**, **PostgreSQL**, **Clean Architecture** and deployed on **AWS**.

---

## 📌 Overview

MedlyCare is a backend system designed to manage clinics, patients, appointments and healthcare workflows.

This project is a modern evolution of a legacy system originally built in Delphi, now restructured using modern backend practices.

### Key highlights

* Multi-tenant system (**clinic/company isolation**)
* Secure authentication with **bcrypt (pgcrypto) + JWT**
* Role-based access control (**RBAC**)
* Audit logging with **JSON (old/new values)**
* Observability with **Serilog + OpenTelemetry**
* Cloud-ready deployment using **Docker + AWS**

> ℹ️ Note: This repository contains only the backend API.

---

## 🛠️ Tech Stack

* ASP.NET Core 8 (REST API)
* Entity Framework Core 8
* PostgreSQL 16
* pgcrypto (password hashing)
* Serilog (structured logging)
* OpenTelemetry (tracing & metrics)
* MediatR (CQRS pattern)
* FluentValidation
* Docker Compose

---

## ☁️ Cloud & Infrastructure

The application is designed to run in AWS environments:

* ECS (container orchestration)
* RDS (PostgreSQL database)
* ECR (container registry)

---

## ⚙️ Setup & Running Locally

### 1. Start database (PostgreSQL + pgAdmin)

```bash
docker compose up -d
```

### 2. Run the application

```bash
dotnet restore
dotnet build
dotnet run --project src/SFA.Api/SFA.Api.csproj
```

### 3. Access Swagger

```
http://localhost:5041/swagger
```

---

## 🧪 Migrations (first time)

```bash
dotnet tool install --global dotnet-ef

dotnet ef migrations add InitialCreate \
-p src/SFA.Infrastructure/SFA.Infrastructure.csproj \
-s src/SFA.Api/SFA.Api.csproj

dotnet ef database update \
-p src/SFA.Infrastructure/SFA.Infrastructure.csproj \
-s src/SFA.Api/SFA.Api.csproj
```

---

## 📂 Project Structure

```
src/
 ├── SFA.Api/              → ASP.NET Core (Swagger, JWT, Serilog)
 ├── SFA.Application/      → Use cases, validations, CQRS
 ├── SFA.Domain/           → Entities and business rules
 └── SFA.Infrastructure/   → EF Core, DbContext, migrations

scripts/sql/               → PostgreSQL scripts (extensions, auditing)
docker-compose.yml         → Local infrastructure
```

---

## 🔒 Security

* Authentication with bcrypt (`pgcrypto`)
* JWT-based authorization
* Role-based access control (RBAC)
* Multi-tenant isolation (`cod_empresa`)
* Optional Row-Level Security (RLS)
* Full audit logging with JSON (old/new values)

---

## 📊 Roadmap (Backend MVP)

* ✅ Initial setup (Docker, migrations, base structure)
* ✅ Authentication (JWT + RBAC)
* ✅ Clinics and roles management
* ✅ Patients and attachments
* ✅ Appointments and workflows
* 🚧 Reporting (summary & detailed)
* 🚧 Full audit system
* 🚧 Testing, CI/CD and production deployment

---

## 🧠 What this project demonstrates

* Clean Architecture in real-world application
* Multi-tenant backend design
* Secure authentication and authorization
* Relational database modeling
* Cloud-ready infrastructure
* Observability and logging

---

## 📄 License

This project is licensed under MIT – see [LICENSE](LICENSE) for details.
