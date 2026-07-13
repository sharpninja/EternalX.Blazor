FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/EternalX.Blazor.Server/EternalX.Blazor.Server.csproj", "src/EternalX.Blazor.Server/"]
COPY ["src/EternalX.Blazor.Client/EternalX.Blazor.Client.csproj", "src/EternalX.Blazor.Client/"]
COPY ["src/EternalX.Blazor.Shared/EternalX.Blazor.Shared.csproj", "src/EternalX.Blazor.Shared/"]
RUN dotnet restore "src/EternalX.Blazor.Server/EternalX.Blazor.Server.csproj"

COPY . .
RUN dotnet publish "src/EternalX.Blazor.Server/EternalX.Blazor.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data \
    && chown -R "$APP_UID:$APP_UID" /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
ENV LITEDB_PATH=/app/data/eternalx.db
VOLUME ["/app/data"]

ENTRYPOINT ["dotnet", "EternalX.Blazor.Server.dll"]

HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
