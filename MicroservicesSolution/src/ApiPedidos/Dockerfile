
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Se recomienda ejecutar el contenedor con un usuario no-root por seguridad.
USER app
WORKDIR /app
# Exponemos el puerto 8080, que es el que usará la aplicación dentro del contenedor.
EXPOSE 8080

# --------------------
# ETAPA 2: COMPILACIÓN (Build)
# Usamos la imagen completa del SDK de .NET 8.0 para compilar el proyecto.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ["ApiPedidos.csproj", "."]
RUN dotnet restore "./ApiPedidos.csproj"

 
COPY . .
 
RUN dotnet build "ApiPedidos.csproj" -c Release -o /app/build

# --------------------
# ETAPA 3: PUBLICACIÓN (Publish)
# Tomamos la etapa de compilación anterior como base.
FROM build AS publish

RUN dotnet publish "ApiPedidos.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ApiPedidos.dll"]
