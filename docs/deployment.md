# Deployment and operations

## Docker Compose deployment

### Initial deployment

```bash
cp .env.example .env
# edit .env and set secure values (at minimum POSTGRES_PASSWORD)
docker compose pull
docker compose up -d
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
| `BRIDGEARR_VERSION` | | GHCR image tag (`stable` by default) |
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
docker compose up -d
docker image prune -f
```

## Automated releases and Dockhand deployment

Merges to `master` are released automatically after CI and CodeQL succeed.
The merged pull request may carry one of `release:major`, `release:minor`, or
`release:patch`; without a release label, patch is used. The first release is
`v0.1.0`.

The workflow publishes a multi-platform candidate to GHCR, points `stable` at
the candidate, synchronizes and redeploys the Dockhand Git stack, and verifies
`/health`. Only a healthy deployment receives immutable `vX.Y.Z` and `latest`
tags and a GitHub Release. A failed deployment restores the previous `stable`
digest and redeploys it automatically.

Configure a GitHub Environment named `production` with secret
`DOCKHAND_TOKEN` and variables `DOCKHAND_URL`, `DOCKHAND_ENV_ID`,
`DOCKHAND_GIT_STACK_ID`, `DOCKHAND_STACK_NAME`, and
`BRIDGEARR_HEALTH_URL`. Repository variable `AUTO_DEPLOY_ENABLED` must remain
`false` during bootstrap and be changed to `true` after the first successful
manual run of **Release and deploy**.

The GHCR package must be public so Dockhand can pull it without registry
credentials. Making a package public is permanent on GitHub.

To redeploy an existing release, set `BRIDGEARR_VERSION` to its immutable
`vX.Y.Z` tag in Dockhand and redeploy the stack. Change it back to `stable` to
resume automated releases.

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
docker compose pull
docker compose up -d
```
