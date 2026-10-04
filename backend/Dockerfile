# Image de l'API SmartRoute. Contexte de build : backend/
#   docker build -t smartroute-api ./backend
# Configuration attendue à l'exécution (variables d'environnement) :
#   Jwt__SigningKey, MongoDb__ConnectionString, MongoDb__DatabaseName

# ---------- Étape 1 : compilation ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restauration seule d'abord : couche mise en cache tant que le .csproj ne change pas.
COPY SmartRoute.Api/SmartRoute.Api.csproj SmartRoute.Api/
RUN dotnet restore SmartRoute.Api/SmartRoute.Api.csproj

COPY SmartRoute.Api/ SmartRoute.Api/
RUN dotnet publish SmartRoute.Api/SmartRoute.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Étape 2 : exécution ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Utilisateur non root fourni par l'image .NET.
USER $APP_UID
# Port HTTP par défaut des images ASP.NET Core.
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartRoute.Api.dll"]
