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

#### Initial deployment

```bash
# Clone the repository
git clone https://github.com/twids/BridgeArr.git
cd BridgeArr

# Create your environment file from the example
cp .env.example .env

# Edit .env and set secure values for all variables
# (at minimum, change POSTGRES_PASSWORD)

# Build and start all services in detached mode
docker compose up -d --build

# Confirm all containers are running and healthy
docker compose ps

# Verify the health endpoint responds
curl http://localhost:8080/health
```

The stack starts:
- `bridgearr` — application on port **8080**
- `postgres` — PostgreSQL (internal only; not exposed to the host)

Database migrations and the default admin account are applied automatically on startup.

#### Default credentials

- **Username:** `admin`
- **Password:** `admin`

You will be prompted to change the password on first login.

#### Environment configuration

Copy `.env.example` to `.env` and adjust the values before the first `docker compose up`.

| Variable | Required | Description |
|---|---|---|
| `POSTGRES_DB` | ✅ | PostgreSQL database name |
| `POSTGRES_USER` | ✅ | PostgreSQL username |
| `POSTGRES_PASSWORD` | ✅ | PostgreSQL password — **use a strong value in production** |
| `ASPNETCORE_ENVIRONMENT` | | Runtime environment (`Production` by default) |
| `PLEX_URL` | | Base URL of your Plex Media Server |
| `PLEX_TOKEN` | | Plex authentication token |
| `RADARR_URL` | | Base URL of your Radarr instance |
| `RADARR_APIKEY` | | Radarr API key |
| `SONARR_URL` | | Base URL of your Sonarr instance |
| `SONARR_APIKEY` | | Sonarr API key |

The `ConnectionStrings__DefaultConnection` is constructed automatically from the `POSTGRES_*` variables, so you do not need to set it manually.

#### Updating containers

```bash
# Pull or rebuild updated images
docker compose pull        # for upstream images (postgres)
docker compose build       # rebuild the bridgearr image

# Re-create containers with the new images (zero downtime for postgres)
docker compose up -d --build

# Remove old images no longer referenced by any container
docker image prune -f
```

#### Database backup and restore

**Backup**

```bash
# Replace 'bridgearr' with the value of POSTGRES_USER and POSTGRES_DB in your .env
docker compose exec postgres pg_dump -U bridgearr bridgearr > backup_$(date +%Y%m%d_%H%M%S).sql
```

**Restore**

```bash
# Stop the application first so no writes occur during restore
docker compose stop bridgearr

# Restore from a backup file
docker compose exec -T postgres psql -U bridgearr bridgearr < backup_YYYYMMDD_HHMMSS.sql

# Restart the application
docker compose start bridgearr
```

#### Troubleshooting

**View live logs**

```bash
docker compose logs -f bridgearr
docker compose logs -f postgres
```

**Container is not healthy / application won't start**

1. Check logs: `docker compose logs bridgearr`
2. Verify the `.env` file contains all required variables and the password is set.
3. Confirm PostgreSQL is healthy before BridgeArr tries to connect:
   ```bash
   docker compose ps postgres
   ```

**Reset everything (⚠️ destroys all data)**

```bash
docker compose down -v
docker compose up -d --build
```

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
