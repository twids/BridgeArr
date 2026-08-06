# Deployment and operations

## Docker Compose deployment

### Initial deployment

```bash
cp .env.example .env
# edit .env and set secure values (at minimum POSTGRES_PASSWORD)
docker compose up -d --build
docker compose ps
curl http://localhost:8080/health
```

Services started:

- `bridgearr` on the host port configured by `BRIDGEARR_PORT` (`8080` by default)
- `postgres` on the internal Docker network

Reverse-proxy configuration is intentionally kept outside this Compose file.
Configure the proxy in the deployment platform and route it to BridgeArr's
published host port, or use a deployment-owned Compose override to attach the
`bridgearr` service to the proxy network and route to container port `8080`.

Database migrations and default admin account seeding run automatically on startup.

### Default credentials

- Username: `admin`
- Password: `admin`

You will be required to change the password on first login.

## Environment configuration

Configure `.env` (see `.env.example`) before first startup.

| Variable | Required | Description |
|---|---|---|
| `POSTGRES_DB` | ✅ | PostgreSQL database name |
| `POSTGRES_USER` | ✅ | PostgreSQL username |
| `POSTGRES_PASSWORD` | ✅ | PostgreSQL password (use a strong value) |
| `ASPNETCORE_ENVIRONMENT` | | Runtime environment (`Production` by default) |
| `BRIDGEARR_PORT` | | Published host port (`8080` by default) |
| `PLEX_URL` | | Plex Media Server base URL |
| `PLEX_TOKEN` | | Plex authentication token |
| `RADARR_URL` | | Radarr base URL |
| `RADARR_APIKEY` | | Radarr API key |
| `SONARR_URL` | | Sonarr base URL |
| `SONARR_APIKEY` | | Sonarr API key |

`ConnectionStrings__DefaultConnection` is assembled from `POSTGRES_*` values in `docker-compose.yml`.

## Updating

```bash
docker compose pull
docker compose build
docker compose up -d --build
docker image prune -f
```

## Backup and restore

### Backup

```bash
docker compose exec postgres pg_dump -U bridgearr bridgearr > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restore

```bash
docker compose stop bridgearr
docker compose exec -T postgres psql -U bridgearr bridgearr < backup_YYYYMMDD_HHMMSS.sql
docker compose start bridgearr
```

## Webhook endpoints

Register these webhook URLs in *Arr applications:

| Source | URL |
|---|---|
| Radarr | `POST http://bridgearr:8080/api/webhooks/radarr` |
| Sonarr | `POST http://bridgearr:8080/api/webhooks/sonarr` |

Set header `X-Webhook-Event` to the event type (for example `Download` or `Rename`).

## Troubleshooting

### View logs

```bash
docker compose logs -f bridgearr
docker compose logs -f postgres
```

### App not healthy / startup issues

1. Check logs: `docker compose logs bridgearr`
2. Verify required `.env` values are set.
3. Confirm PostgreSQL is healthy: `docker compose ps postgres`

### Full reset (destroys all data)

```bash
docker compose down -v
docker compose up -d --build
```
