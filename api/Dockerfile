# ============================================
# Dockerfile — Monrys.Api (.NET 9)
# Desplegado en Koyeb (plan Nano gratuito)
# ============================================

# --------------------------------------------
# STAGE 1: Build
# --------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar solo el archivo de proyecto para restaurar dependencias (capa de cache)
COPY ["Monrys.Api.csproj", "./"]
RUN dotnet restore "Monrys.Api.csproj"

# Copiar el resto del código fuente
COPY . ./

# Publicar en modo Release
RUN dotnet publish "Monrys.Api.csproj" -c Release -o /app/publish --no-restore

# --------------------------------------------
# STAGE 2: Runtime (imagen final ligera)
# --------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Variables de entorno por defecto (se sobrescriben con las de Koyeb)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Copiar los archivos publicados desde el stage de build
COPY --from=build /app/publish .

# Puerto expuesto (Koyeb inyecta la variable PORT)
EXPOSE ${PORT:-8080}

# Comando de inicio
ENTRYPOINT ["dotnet", "Monrys.Api.dll"]
