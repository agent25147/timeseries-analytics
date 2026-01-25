# Production Deployment Guide

## 🏗️ Architecture Overview

```
User Browser
    ↓
Frontend (Nginx on Port 80)
    ├─→ /           → Angular App (Static Files)
    └─→ /api/*      → Proxy to Backend (Port 8080)
            ↓
    Backend (.NET API)
            ↓
    ClickHouse Database
```

**Key Design Decision**: The frontend uses a **relative path** (`/api`) for API calls, and Nginx proxies these to the backend container. This means:
- ✅ Works in any environment (local, staging, production)
- ✅ No CORS issues (same origin)
- ✅ No hardcoded URLs
- ✅ Simplified deployment

---

## 📦 What to Give DevOps

### Option 1: Source Code + Docker Compose (Recommended for initial deployment)

Give them this repository with these files:

```
timeseries-analytics/
├── docker-compose.yml          # Orchestration file
├── backend/
│   ├── Dockerfile              # Backend image definition
│   └── TimeSeriesAnalytics.Api/
├── frontend/
│   ├── Dockerfile              # Frontend image definition
│   ├── nginx.conf              # Nginx reverse proxy config
│   └── timeseries-analytics-ui/
└── clickhouse/
    └── config/
        └── users.xml           # ClickHouse user config
```

**Instructions for DevOps**:
```bash
# 1. Clone the repository
git clone <repo-url>
cd timeseries-analytics

# 2. Build and run
docker-compose up -d --build

# 3. Verify
docker-compose ps
docker-compose logs -f

# 4. Access
# Frontend: http://<server-ip>:4200
# Backend API: http://<server-ip>:5000/api
# ClickHouse: http://<server-ip>:8123
```

### Option 2: Pre-built Docker Images (Recommended for production)

1. **Build and push images to Docker registry**:

```bash
# Login to your Docker registry (Docker Hub, AWS ECR, Azure ACR, etc.)
docker login

# Build and tag images
docker build -t your-registry/ts-analytics-backend:v1.0.0 ./backend
docker build -t your-registry/ts-analytics-frontend:v1.0.0 ./frontend

# Push to registry
docker push your-registry/ts-analytics-backend:v1.0.0
docker push your-registry/ts-analytics-frontend:v1.0.0
```

2. **Provide docker-compose.prod.yml**:

```yaml
version: '3.8'

services:
  clickhouse:
    image: clickhouse/clickhouse-server:latest
    container_name: ts-analytics-clickhouse
    ports:
      - "8123:8123"
      - "9000:9000"
    environment:
      - CLICKHOUSE_DB=analytics
      - CLICKHOUSE_USER=demo_user
      - CLICKHOUSE_PASSWORD=${CLICKHOUSE_PASSWORD}  # Use secrets in production
    volumes:
      - clickhouse-data:/var/lib/clickhouse
    networks:
      - ts-network
    restart: unless-stopped

  backend:
    image: your-registry/ts-analytics-backend:v1.0.0
    container_name: ts-analytics-backend
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ClickHouse__Host=clickhouse
      - ClickHouse__Port=8123
      - ClickHouse__Database=analytics
      - ClickHouse__Username=demo_user
      - ClickHouse__Password=${CLICKHOUSE_PASSWORD}
    depends_on:
      - clickhouse
    networks:
      - ts-network
    restart: unless-stopped

  frontend:
    image: your-registry/ts-analytics-frontend:v1.0.0
    container_name: ts-analytics-frontend
    ports:
      - "80:80"  # Production on port 80
    depends_on:
      - backend
    networks:
      - ts-network
    restart: unless-stopped

networks:
  ts-network:
    driver: bridge

volumes:
  clickhouse-data:
```

---

## 🌐 Production Deployment Scenarios

### Scenario 1: Single Server Deployment

**Domain**: `analytics.yourcompany.com`

**Setup**:
1. Point DNS to server IP
2. Update `nginx.conf` server_name to your domain
3. Deploy with docker-compose
4. Frontend will call `/api` → Nginx proxies to backend

**User accesses**: `https://analytics.yourcompany.com`
- Frontend served from: `https://analytics.yourcompany.com/`
- API calls go to: `https://analytics.yourcompany.com/api/*` (proxied to backend)

### Scenario 2: Separate Domains (If Needed)

**Frontend**: `app.yourcompany.com`
**Backend**: `api.yourcompany.com`

In this case, update `environment.prod.ts`:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.yourcompany.com/api'
};
```

And update CORS in `appsettings.json`:
```json
"Cors": {
  "AllowedOrigins": [
    "https://app.yourcompany.com",
    "http://localhost:4200"
  ]
}
```

### Scenario 3: Cloud Deployment (AWS/Azure/GCP)

#### AWS Example (ECS + ALB):

```
Internet
    ↓
Application Load Balancer (ALB)
    ├─→ /          → ECS Service (Frontend)
    └─→ /api/*     → ECS Service (Backend)
            ↓
        RDS for ClickHouse or EC2
```

#### Kubernetes Example:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: frontend
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 80
  selector:
    app: frontend

---
apiVersion: v1
kind: Service
metadata:
  name: backend
spec:
  type: ClusterIP
  ports:
  - port: 8080
  selector:
    app: backend
```

---

## 🔐 Production Checklist

### Security
- [ ] Change default ClickHouse credentials
- [ ] Use environment variables for secrets (never commit)
- [ ] Enable HTTPS/SSL (use Let's Encrypt or cloud certificates)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Enable firewall rules (only expose port 80/443)
- [ ] Regular security updates for base images

### Performance
- [ ] Enable Redis caching if needed
- [ ] Set up CDN for static assets
- [ ] Configure database connection pooling
- [ ] Set appropriate memory/CPU limits in docker-compose

### Monitoring
- [ ] Add health check endpoints
- [ ] Set up logging (ELK stack, CloudWatch, etc.)
- [ ] Configure alerts for failures
- [ ] Monitor disk space (ClickHouse data grows)

### Backup
- [ ] Regular ClickHouse data backups
- [ ] Backup docker volumes
- [ ] Document restore procedures

---

## 🚀 Deployment Commands

### Local Testing (with production build)
```bash
docker-compose up --build -d
```

### Production Deployment
```bash
# Pull latest images
docker-compose pull

# Stop current containers
docker-compose down

# Start with new images
docker-compose up -d

# View logs
docker-compose logs -f
```

### Health Checks
```bash
# Backend health
curl http://localhost:5000/api/timeseries/health

# Database connection
curl http://localhost:8123/ping

# Frontend
curl http://localhost:4200
```

---

## 📊 Scaling Considerations

### Horizontal Scaling
- Use Docker Swarm or Kubernetes
- Multiple backend instances behind load balancer
- Stateless backend design allows easy scaling

### Database Scaling
- ClickHouse clustering for large datasets
- Read replicas for query performance
- Partitioning by date for time-series data

---

## 🔄 CI/CD Pipeline Example

```yaml
# .github/workflows/deploy.yml
name: Deploy to Production

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Build Backend
        run: docker build -t ${{ secrets.REGISTRY }}/backend:${{ github.sha }} ./backend
      
      - name: Build Frontend
        run: docker build -t ${{ secrets.REGISTRY }}/frontend:${{ github.sha }} ./frontend
      
      - name: Push Images
        run: |
          docker push ${{ secrets.REGISTRY }}/backend:${{ github.sha }}
          docker push ${{ secrets.REGISTRY }}/frontend:${{ github.sha }}
      
      - name: Deploy to Server
        run: |
          ssh ${{ secrets.SERVER_USER }}@${{ secrets.SERVER_IP }} \
            "cd /opt/timeseries-analytics && \
             docker-compose pull && \
             docker-compose up -d"
```

---

## 📞 Support Information

### Common Issues

**Issue**: Frontend shows "Cannot connect to API"
- Check: `docker-compose logs backend`
- Verify: Backend container is running
- Test: `curl http://localhost:5000/api/timeseries/health`

**Issue**: CORS errors
- Check: Backend logs for CORS policy
- Verify: Frontend origin is in allowed origins list

**Issue**: ClickHouse connection failed
- Check: `docker-compose logs clickhouse`
- Verify: Database credentials match in all services

### Log Locations
- Frontend: `docker-compose logs frontend`
- Backend: `docker-compose logs backend`
- Database: `docker-compose logs clickhouse`

---

## 📝 Environment Variables Reference

### Backend
| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | Development | Yes |
| `ClickHouse__Host` | Database host | localhost | Yes |
| `ClickHouse__Port` | Database port | 8123 | Yes |
| `ClickHouse__Database` | Database name | analytics | Yes |
| `ClickHouse__Username` | DB username | demo_user | Yes |
| `ClickHouse__Password` | DB password | demo_pass | Yes |

### Frontend
| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `API_URL` | Backend API URL | (set in environment.ts) | No* |

*API_URL in docker-compose is currently unused. API routing is handled by Nginx proxy to relative path `/api`.

---

## 🎯 Summary for DevOps

**What you're deploying**: 
- Angular frontend (Nginx)
- .NET 8 backend API
- ClickHouse database

**How it works**:
- User visits your domain
- Nginx serves Angular app
- API calls to `/api/*` are proxied to backend container
- Backend connects to ClickHouse for data

**What to configure**:
1. Domain name in Nginx config
2. SSL certificate (use Let's Encrypt)
3. ClickHouse password (use secrets)
4. Resource limits based on usage
5. Backup strategy for ClickHouse data

**Ports needed**:
- 80/443 (HTTP/HTTPS) - Public
- 5000 (Backend API) - Optional, for direct access
- 8123 (ClickHouse HTTP) - Internal only
- 9000 (ClickHouse native) - Internal only
