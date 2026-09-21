# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto
COPY TFU_Architecture_Tactics.csproj .
COPY *.cs ./

# Restaurar dependencias
RUN dotnet restore TFU_Architecture_Tactics.csproj

# Copiar código fuente
COPY . .

# Compilar la aplicación
RUN dotnet publish TFU_Architecture_Tactics.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# curl es usado por el healthcheck de docker-compose
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copiar compilados del stage anterior
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 3000

# Comando para iniciar la aplicación
ENTRYPOINT ["dotnet", "TFU_Architecture_Tactics.dll"]
