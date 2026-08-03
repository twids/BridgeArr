ARG DOTNET_VERSION=10.0
ARG DOTNET_DISTRO_VARIANT=noble

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-${DOTNET_DISTRO_VARIANT} AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup --no-create-home appuser

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-${DOTNET_DISTRO_VARIANT} AS build
WORKDIR /src
COPY ["src/BridgeArr.Web/BridgeArr.Web.csproj", "src/BridgeArr.Web/"]
COPY ["src/BridgeArr.Api/BridgeArr.Api.csproj", "src/BridgeArr.Api/"]
COPY ["src/BridgeArr.Application/BridgeArr.Application.csproj", "src/BridgeArr.Application/"]
COPY ["src/BridgeArr.Domain/BridgeArr.Domain.csproj", "src/BridgeArr.Domain/"]
COPY ["src/BridgeArr.Infrastructure/BridgeArr.Infrastructure.csproj", "src/BridgeArr.Infrastructure/"]
COPY ["src/BridgeArr.Plugins.Abstractions/BridgeArr.Plugins.Abstractions.csproj", "src/BridgeArr.Plugins.Abstractions/"]
COPY ["src/BridgeArr.Plugins.Radarr/BridgeArr.Plugins.Radarr.csproj", "src/BridgeArr.Plugins.Radarr/"]
COPY ["src/BridgeArr.Plugins.Sonarr/BridgeArr.Plugins.Sonarr.csproj", "src/BridgeArr.Plugins.Sonarr/"]
COPY ["src/BridgeArr.Plugins.Plex/BridgeArr.Plugins.Plex.csproj", "src/BridgeArr.Plugins.Plex/"]
RUN dotnet restore "src/BridgeArr.Web/BridgeArr.Web.csproj"
COPY . .
WORKDIR "/src/src/BridgeArr.Web"
RUN dotnet build "BridgeArr.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BridgeArr.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/logs /app/dataprotection \
    && chown -R appuser:appgroup /app
USER appuser

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BridgeArr.Web.dll"]
