# --------------------
# STAGE 1: BASE IMAGE (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

# --------------------
# STAGE 2: BUILD
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["ApiPago.csproj", "."]
RUN dotnet restore "./ApiPago.csproj"

# Copiar el resto del código fuente y construir
COPY . .
RUN dotnet build "ApiPago.csproj" -c Release -o /app/build

# --------------------
# STAGE 3: PUBLISH
FROM build AS publish
RUN dotnet publish "ApiPago.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --------------------
# STAGE 4: FINAL IMAGE
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiPago.dll"]