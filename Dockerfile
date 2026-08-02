FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
ENTRYPOINT ["dotnet", "BridgeArr.Web.dll"]
