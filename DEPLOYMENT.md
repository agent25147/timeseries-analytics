# TimeS eries Analytics - Deployment Guide

## Prerequisites
- Docker Desktop installed and running
- Docker Compose installed (comes with Docker Desktop)

## Deployment Steps

### 1. Build and Start All Services

From the project root directory, run:

```bash
docker-compose up --build -d
```

This will:
- Build the backend (.NET 8 API)
- Build the frontend (Angular app)
- Pull and start ClickHouse database
- Start all services in detached mode

### 2. Verify Services are Running

```bash
docker-compose ps
```

You should see 3 containers running:
- `ts-analytics-clickhouse` (port 8123, 9000)
- `ts-analytics-backend` (port 5000)
- `ts-analytics-frontend` (port 4200)

### 3. Access the Application

- **Frontend**: http://localhost:4200
- **Backend API**: http://localhost:5000
- **ClickHouse**: http://localhost:8123

### 4. Initialize Data (First Time Only)

Once all services are up, you need to seed the database:

1. Open your browser to http://localhost:4200
2. The backend will automatically create the database schema
3. Use the data seeding endpoint to generate sample data:
   - POST to http://localhost:5000/api/DataSeed/generate
   - Body: `{ "recordCount": 1000 }`

Or use curl:
```bash
curl -X POST http://localhost:5000/api/DataSeed/generate \
  -H "Content-Type: application/json" \
  -d "{\"recordCount\": 1000}"
```

### 5. View Logs

To see logs from all services:
```bash
docker-compose logs -f
```

To see logs from a specific service:
```bash
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f clickhouse
```

### 6. Stop Services

```bash
docker-compose down
```

To stop and remove volumes (deletes all data):
```bash
docker-compose down -v
```

### 7. Rebuild After Code Changes

```bash
docker-compose up --build
```

## Troubleshooting

### Issue: Backend can't connect to ClickHouse
- Wait 10-15 seconds after starting ClickHouse before the backend connects
- Check ClickHouse logs: `docker-compose logs clickhouse`

### Issue: Frontend can't connect to Backend
- Verify backend is running: `docker-compose ps`
- Check backend logs: `docker-compose logs backend`
- Ensure environment variable `API_URL` is correctly set in docker-compose.yml

### Issue: Port already in use
- Change the port mapping in docker-compose.yml
- Example: `"4201:80"` instead of `"4200:80"`

## Production Deployment

For production deployment:

1. Update `docker-compose.yml`:
   - Change `ASPNETCORE_ENVIRONMENT` to `Production`
   - Use stronger passwords for ClickHouse
   - Configure proper API URLs

2. Use a reverse proxy (Nginx/Traefik) for:
   - SSL/TLS termination
   - Load balancing
   - Rate limiting

3. Set up data persistence:
   - ClickHouse data is persisted in Docker volume `clickhouse-data`
   - Configure backups for this volume

4. Monitor with:
   - Docker health checks
   - Application logging
   - ClickHouse monitoring tools

## Architecture

```
┌─────────────────┐
│    Frontend     │  Port 4200
│   (Angular +    │  Nginx
│     Nginx)      │
└────────┬────────┘
         │
         │ HTTP
         ▼
┌─────────────────┐
│     Backend     │  Port 5000
│   (.NET 8 API)  │
└────────┬────────┘
         │
         │ ClickHouse Protocol
         ▼
┌─────────────────┐
│   ClickHouse    │  Ports 8123, 9000
│    Database     │
└─────────────────┘
```

## Container Details

### Frontend Container
- Base: `nginx:alpine`
- Size: ~50MB
- Serves static Angular files
- Gzip compression enabled

### Backend Container
- Base: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Size: ~200MB
- Runs ASP.NET Core API
- Health endpoint: `/health` (if configured)

### ClickHouse Container
- Base: `clickhouse/clickhouse-server:latest`
- Size: ~600MB
- Persistent volume for data
- Custom users configuration

## Environment Variables

### Backend
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ClickHouse__Host`: ClickHouse hostname
- `ClickHouse__Port`: ClickHouse HTTP port
- `ClickHouse__Database`: Database name
- `ClickHouse__Username`: ClickHouse username
- `ClickHouse__Password`: ClickHouse password

### Frontend
- `API_URL`: Backend API URL (used during build)

## Next Steps

After deployment:
1. Configure row grouping and pivot tables
2. Set up filters and sorting
3. Customize column definitions
4. Add more data sources
5. Implement authentication (if needed)
