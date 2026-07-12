FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/EternalX.Blazor.Server/EternalX.Blazor.Server.csproj", "EternalX.Blazor.Server/"]
COPY ["src/EternalX.Blazor.Client/EternalX.Blazor.Client.csproj", "EternalX.Blazor.Client/"]
COPY ["src/EternalX.Blazor.Shared/EternalX.Blazor.Shared.csproj", "EternalX.Blazor.Shared/"]
RUN dotnet restore "EternalX.Blazor.Server/EternalX.Blazor.Server.csproj"

COPY . .
WORKDIR "/src/EternalX.Blazor.Server"
RUN dotnet build "EternalX.Blazor.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EternalX.Blazor.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "EternalX.Blazor.Server.dll"]

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1