# Configuration Summary

## Environment URLs

### Local Development
- Frontend: http://localhost:4200
- Backend: https://localhost:7129 (with SSL) or http://localhost:5226 (without SSL)
- API Endpoint: `https://localhost:7129/api`

### Docker Deployment  
- Frontend: http://localhost:4200
- Backend: http://localhost:5000
- API Endpoint: `http://localhost:5000/api`

## Configuration Files

### Frontend

**Development** (`src/environments/environment.development.ts`):
```typescript
apiUrl: 'https://localhost:7129/api'
```

**Production/Docker** (`src/environments/environment.prod.ts`):
```typescript
apiUrl: 'http://localhost:5000/api'
```

### Backend

**CORS Origins** (`appsettings.json`):
- `http://localhost:4200` - Local dev frontend
- `http://localhost:4201` - Alternative port
- `http://frontend` - Docker network name

**ClickHouse Connection**:
- Local: `localhost:8123`
- Docker: `clickhouse:8123` (set via environment variables in docker-compose.yml)

## Key Changes Made

1. ✅ Created `environment.prod.ts` with Docker API URL
2. ✅ Updated `angular.json` to use prod environment on build
3. ✅ Updated frontend Dockerfile to build with `--configuration production`
4. ✅ Added CORS origins for Docker
5. ✅ Disabled HTTPS redirection in Docker (HTTP only)
6. ✅ Increased Angular bundle size budget for AG Grid Enterprise

## Rebuild Instructions

After these changes, rebuild the containers:

```bash
docker-compose down
docker-compose up --build -d
```

## Verification

1. Check frontend logs: `docker-compose logs frontend`
2. Check backend logs: `docker-compose logs backend`
3. Test API: http://localhost:5000/swagger
4. Open app: http://localhost:4200
