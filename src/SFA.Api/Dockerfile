# ===== build (SDK) =====
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
WORKDIR /src

COPY SFA.sln ./
COPY src/SFA.Api/SFA.Api.csproj           src/SFA.Api/
COPY src/SFA.Domain/SFA.Domain.csproj     src/SFA.Domain/
COPY src/SFA.Infrastructure/SFA.Infrastructure.csproj src/SFA.Infrastructure/
COPY src/SFA.Application/SFA.Application.csproj src/SFA.Application/

RUN dotnet restore ./SFA.sln --nologo

COPY . .
RUN dotnet publish src/SFA.Api/SFA.Api.csproj -c Release -o /app/publish --no-restore

# ===== runtime (ASP.NET) =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble AS runtime
WORKDIR /app

# Dependências úteis (curl p/ healthcheck; ICU p/ Globalization; tzdata p/ timezone)
RUN apt-get update && apt-get install -y --no-install-recommends \
      curl ca-certificates tzdata libicu74 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Porta 8080 para casar com ECS/ALB
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# Healthcheck HTTP interno (bate em /health da sua API)
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "SFA.Api.dll"]
