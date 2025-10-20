# ETAPA 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "BlazorWeb.csproj" -c Release -o /app/publish

# ETAPA 2: Runtime con Nginx
FROM nginx:alpine AS final

# Copiar los archivos estáticos de Blazor Wasm
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Copiar configuración personalizada de nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Exponer puerto 80 (interno de nginx)
EXPOSE 80

# Nginx se inicia automáticamente