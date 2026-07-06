# syntax=docker/dockerfile:1
# Imagen única de la API: .NET 8 + Python 3.11 (worker facial).
# Contexto de build: escenario-3-facial/src/

# ---------- Etapa 1: build .NET ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore cacheable: primero solo los csproj.
COPY services/access-control-api/AccessControl.Api/AccessControl.Api.csproj services/access-control-api/AccessControl.Api/
COPY services/access-control-api/AccessControl.Domain/AccessControl.Domain.csproj services/access-control-api/AccessControl.Domain/
COPY services/access-control-api/AccessControl.Infrastructure/AccessControl.Infrastructure.csproj services/access-control-api/AccessControl.Infrastructure/
RUN dotnet restore services/access-control-api/AccessControl.Api/AccessControl.Api.csproj

COPY services/ services/
RUN dotnet publish services/access-control-api/AccessControl.Api/AccessControl.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- Etapa 2: dependencias Python + modelo ----------
# Misma base Debian que el runtime para que el venv sea 100% compatible.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS python-deps
RUN apt-get update && apt-get install -y --no-install-recommends \
        python3 python3-venv python3-pip python3-dev build-essential \
    && rm -rf /var/lib/apt/lists/*

COPY services/access-control-api/python/requirements.txt /tmp/requirements.txt
RUN python3 -m venv /opt/facial \
    && /opt/facial/bin/pip install --no-cache-dir --upgrade pip \
    && /opt/facial/bin/pip install --no-cache-dir -r /tmp/requirements.txt

# El modelo buffalo_l (~300 MB) se pre-descarga EN BUILD: el primer arranque del
# contenedor no depende de la red y la primera validación es inmediata.
RUN /opt/facial/bin/python -c "from insightface.app import FaceAnalysis; FaceAnalysis(name='buffalo_l', root='/opt/insightface', providers=['CPUExecutionProvider'])"

# ---------- Etapa 3: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Python mínimo para ejecutar el venv (sin toolchain de compilación).
RUN apt-get update && apt-get install -y --no-install-recommends \
        python3 libglib2.0-0 libgomp1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=python-deps /opt/facial /opt/facial
COPY --from=python-deps /opt/insightface /opt/insightface
COPY --from=build /app/publish .
COPY frontend/ ./wwwroot/

ENV ASPNETCORE_URLS=http://+:8080 \
    Facial__PythonEjecutable=/opt/facial/bin/python \
    Facial__WorkerScript=/app/python/facial_worker.py \
    FACIAL_MODELS_DIR=/opt/insightface \
    Almacenamiento__RutaBase=/app/data

EXPOSE 8080
ENTRYPOINT ["dotnet", "AccessControl.Api.dll"]
