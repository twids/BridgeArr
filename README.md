# BridgeArr

BridgeArr is a modular media integration platform that connects applications in the \*Arr ecosystem (Radarr, Sonarr, Lidarr, Readarr etc.) with media servers (Plex, Jellyfin, Emby etc.).

## Features

- **Plugin-based architecture** — Core is decoupled from all external systems
- **Radarr & Sonarr** sources — tag/metadata synchronisation via API and webhooks
- **Plex** target — label synchronisation from \*Arr tags
- **Event-driven sync** — webhooks return immediately; long-running jobs run in background
- **Admin UI** — Blazor Server dashboard for integrations, jobs, logs and settings
- **REST API** — documented with Swagger/OpenAPI
- **PostgreSQL** persistence via Entity Framework Core
- **ASP.NET Core Identity** authentication with forced password change on first login

## Solution structure

```
BridgeArr.sln
src/
  BridgeArr.Domain                  # Core entities, enums — no external dependencies
  BridgeArr.Plugins.Abstractions    # Plugin interfaces (IMediaSource, IMediaTarget, …)
  BridgeArr.Application             # Services, events, repository interfaces
  BridgeArr.Infrastructure          # EF Core, Identity, queue, background worker
  BridgeArr.Plugins.Radarr          # Radarr API client + webhook handler
  BridgeArr.Plugins.Sonarr          # Sonarr API client + webhook handler
  BridgeArr.Plugins.Plex            # Plex API client
  BridgeArr.Api                     # API controllers (mounted into Web host)
  BridgeArr.Web                     # Blazor Server host (combined UI + API entry point)
tests/
  BridgeArr.UnitTests               # Unit tests (no database required)
  BridgeArr.IntegrationTests        # Architecture / integration tests
```

## Development setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/) (for PostgreSQL)

### 1. Start PostgreSQL

```bash
docker compose up postgres -d
```

### 2. Apply database migrations

```bash
cd src/BridgeArr.Infrastructure
dotnet ef database update --startup-project ../BridgeArr.Web
```

### 3. Run the application

```bash
cd src/BridgeArr.Web
dotnet run
```

The application starts at `http://localhost:5000`.

Default credentials:
- **Username:** `admin`
- **Password:** `admin`

You will be prompted to change the password on first login.

### 4. API documentation

Browse to `http://localhost:5000/swagger` while running in Development mode.

### 5. Run tests

```bash
dotnet test
```

### Environment variables (development overrides)

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | See `appsettings.Development.json` | PostgreSQL connection string |

## Production deployment

### Docker Compose (recommended)

```bash
# Clone the repository
git clone https://github.com/twids/BridgeArr.git
cd BridgeArr

# (Optional) override credentials — edit docker-compose.yml or set env vars
# Start all services
docker compose up -d

# Check health
curl http://localhost:8080/health
```

The stack starts:
- `bridgearr` — application on port **8080**
- `postgres` — PostgreSQL on port **5432** (internal only in production)

Database migrations are applied automatically on startup.

### Environment variables (production)

| Variable | Required | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | ✅ | PostgreSQL connection string |
| `ASPNETCORE_ENVIRONMENT` | | `Production` (default in Docker Compose) |

### Webhook endpoints

Register these URLs in your \*Arr applications:

| Source  | URL |
|---------|-----|
| Radarr  | `POST http://bridgearr:8080/api/webhooks/radarr` |
| Sonarr  | `POST http://bridgearr:8080/api/webhooks/sonarr` |

Set the `X-Webhook-Event` header to the event type (e.g. `Download`, `Rename`).

## Adding a new plugin

1. Create a class library `src/BridgeArr.Plugins.MyApp`
2. Reference `BridgeArr.Plugins.Abstractions`
3. Implement `IMediaSource` or `IMediaTarget` (and optionally `IWebhookHandler`)
4. Register the plugin in `DependencyInjection.cs`
5. Call `services.AddMyAppPlugin()` in `BridgeArr.Web/Program.cs`

The core application discovers plugins through the DI container — no changes to core code are required.

## License

MIT
