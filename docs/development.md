# Development setup

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)

## 1) Start PostgreSQL

```bash
docker compose up postgres -d
```

## 2) Apply database migrations

```bash
cd src/BridgeArr.Infrastructure
dotnet ef database update --startup-project ../BridgeArr.Web
```

## 3) Run the application

```bash
cd src/BridgeArr.Web
dotnet run
```

The application starts at `http://localhost:5000`.

Default credentials:

- Username: `admin`
- Password: `admin`

You will be required to change the password on first login.

## 4) View API docs

While running in Development mode, open:

- `http://localhost:5000/swagger`

## 5) Run tests

```bash
dotnet test
```

## Environment variables

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | from `src/BridgeArr.Web/appsettings.Development.json` | PostgreSQL connection string |
