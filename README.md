# BridgeArr

BridgeArr is a modular media integration platform that connects applications in the *Arr ecosystem (Radarr, Sonarr, Lidarr, Readarr, etc.) with media servers (Plex, Jellyfin, Emby, etc.).

## Features

- Plugin-based architecture with decoupled source/target integrations
- Radarr and Sonarr source plugins with API + webhook support
- Plex target plugin for label synchronization
- Event-driven background job processing
- Blazor Server admin UI for integrations, jobs, logs, and settings
- REST API with Swagger/OpenAPI in development
- PostgreSQL persistence via Entity Framework Core
- ASP.NET Core Identity authentication with forced password change on first login

## Documentation

- [Development setup](./docs/development.md)
- [Deployment and operations](./docs/deployment.md)

## Quick start (Docker Compose)

```bash
cp .env.example .env
# edit .env and set secure values, especially POSTGRES_PASSWORD
docker compose up -d --build
```

Then open `http://localhost:8080` and log in with:

- Username: `admin`
- Password: `admin`

You will be required to change the password on first login.

## Solution structure

```text
BridgeArr.sln
src/
  BridgeArr.Domain
  BridgeArr.Plugins.Abstractions
  BridgeArr.Application
  BridgeArr.Infrastructure
  BridgeArr.Plugins.Radarr
  BridgeArr.Plugins.Sonarr
  BridgeArr.Plugins.Plex
  BridgeArr.Api
  BridgeArr.Web
tests/
  BridgeArr.UnitTests
  BridgeArr.IntegrationTests
```

## License

MIT
