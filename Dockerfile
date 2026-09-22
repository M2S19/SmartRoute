# ============================
# ÉTAPE 1 : COMPILATION
# ============================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["SmartRoute.API.csproj", "./"]

RUN dotnet restore "SmartRoute.API.csproj"

COPY . .

RUN dotnet publish "SmartRoute.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ============================
# ÉTAPE 2 : EXÉCUTION
# ============================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE ${API_INTERNAL_PORT}

ENTRYPOINT ["dotnet", "SmartRoute.API.dll"]