# ============================================
# Dockerfile - Monrys.Api (.NET 9)
# Compatible con cualquier PaaS de contenedores
# (Render, Koyeb, Fly.io, Cloud Run, VPS...)
# ============================================

# --------------------------------------------
# STAGE 1: Build
# --------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar solo el archivo de proyecto para restaurar dependencias (capa de cache)
COPY ["Monrys.Api.csproj", "./"]
RUN dotnet restore "Monrys.Api.csproj"

# Copiar el resto del codigo fuente
COPY . ./

# Publicar en modo Release
RUN dotnet publish "Monrys.Api.csproj" -c Release -o /app/publish --no-restore

# --------------------------------------------
# STAGE 2: Runtime (imagen final ligera)
# --------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

COPY --from=build /app/publish .

# El puerto se resuelve en RUNTIME: la mayoria de PaaS inyectan PORT
# (Render usa 10000 por defecto); si no existe, 8080.
# No se declara EXPOSE a proposito: Render deduce el puerto de PORT cuando
# el Dockerfile no expone ninguno, y asi ambos valores no pueden divergir.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet Monrys.Api.dll"]
