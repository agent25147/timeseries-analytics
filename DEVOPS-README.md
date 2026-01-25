# DevOps Quick Start Guide

## 📦 What You're Getting

A complete time-series analytics application with:
- **Frontend**: Angular + AG Grid (port 80)
- **Backend**: .NET 8 Web API (port 5000)
- **Database**: ClickHouse (ports 8123, 9000)

All containerized with Docker Compose.

---

## 🚀 Quick Deployment (5 minutes)

### Prerequisites
- Docker & Docker Compose installed
- Ports 80, 5000, 8123, 9000 available

### Steps

1. **Clone the repository**
```bash
git clone <repo-url>
cd timeseries-analytics
```

2. **Configure environment**
```bash
# Copy environment template
cp .env.example .env

# Edit .env file (IMPORTANT: Change default password!)
nano .env
```

3. **Deploy**
```bash
# Windows
deploy-prod.bat

# Linux/Mac
docker-compose -f docker-compose.prod.yml up -d --build
```

4. **Verify**
- Frontend: http://localhost
- Backend API: http://localhost:5000/swagger
- Health Check: http://localhost:5000/api/timeseries/health

---

## 🏗️ How It Works

```
User Request → Frontend (Nginx:80)
                  ↓
              /api/* → Backend (.NET:8080) → ClickHouse:8123
                  ↓
              /      → Angular App (Static)
```

**Key Feature**: Frontend uses relative path `/api` which Nginx proxies to backend. This means:
- ✅ No hardcoded URLs
- ✅ Works in any environment
- ✅ No CORS issues
- ✅ Single domain deployment

---

## 🔐 Production Checklist

### Before Going Live

1. **Security**
   - [ ] Change `CLICKHOUSE_PASSWORD` in `.env`
   - [ ] Set `ASPNET_ENVIRONMENT=Production`
   - [ ] Configure firewall (allow only 80/443)
   - [ ] Set up SSL/TLS (see SSL Setup below)

2. **Monitoring**
   - [ ] Set up log aggregation
   - [ ] Configure health check monitoring
   - [ ] Set up alerts for service failures

3. **Backup**
   - [ ] Configure ClickHouse data backup
   - [ ] Document restore procedures
   - [ ] Test backup/restore process

---

## 🌐 SSL/HTTPS Setup

### Option 1: Using Reverse Proxy (Recommended)

Deploy Nginx or Traefik in front:

```yaml
# Add to docker-compose.prod.yml
  nginx-proxy:
    image: nginx:alpine
    ports:
      - "443:443"
      - "80:80"
    volumes:
      - ./ssl-config.conf:/etc/nginx/nginx.conf
      - /etc/letsencrypt:/etc/letsencrypt
```

### Option 2: Let's Encrypt with Certbot

```bash
# Install certbot
apt-get install certbot

# Get certificate
certbot certonly --standalone -d yourdomain.com

# Configure nginx.conf to use certs
# /etc/letsencrypt/live/yourdomain.com/fullchain.pem
# /etc/letsencrypt/live/yourdomain.com/privkey.pem
```

---

## 📊 Scaling & Performance

### Horizontal Scaling

```yaml
# docker-compose.prod.yml
backend:
  deploy:
    replicas: 3  # Run 3 backend instances
```

Add load balancer (Nginx/Traefik) to distribute traffic.

### Resource Limits

```yaml
backend:
  deploy:
    resources:
      limits:
        cpus: '2'
        memory: 4G
      reservations:
        cpus: '1'
        memory: 2G
```

### Database Optimization

- Enable ClickHouse compression (already enabled)
- Partition tables by date for large datasets
- Configure buffer sizes in ClickHouse config

---

## 🔍 Monitoring & Logs

### View Logs
```bash
# All services
docker-compose -f docker-compose.prod.yml logs -f

# Specific service
docker-compose -f docker-compose.prod.yml logs -f backend

# Last 100 lines
docker-compose -f docker-compose.prod.yml logs --tail=100
```

### Health Checks
```bash
# Backend health
curl http://localhost:5000/api/timeseries/health

# ClickHouse health
curl http://localhost:8123/ping

# Frontend health
curl http://localhost
```

### Service Status
```bash
docker-compose -f docker-compose.prod.yml ps
```

---

## 🛠️ Common Operations

### Update Application
```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose -f docker-compose.prod.yml up -d --build
```

### Restart Services
```bash
# All services
docker-compose -f docker-compose.prod.yml restart

# Specific service
docker-compose -f docker-compose.prod.yml restart backend
```

### Scale Backend
```bash
docker-compose -f docker-compose.prod.yml up -d --scale backend=3
```

### Backup Database
```bash
# Backup ClickHouse data
docker exec ts-analytics-clickhouse clickhouse-client --query="BACKUP DATABASE analytics TO Disk('default', 'backup_$(date +%Y%m%d).zip')"

# Or backup volume
docker run --rm -v ts-analytics_clickhouse-data:/data -v $(pwd):/backup alpine tar czf /backup/clickhouse-backup.tar.gz /data
```

---

## 🐛 Troubleshooting

### Frontend Can't Connect to Backend

**Symptoms**: Network errors in browser console

**Check**:
1. Backend is running: `docker-compose -f docker-compose.prod.yml ps`
2. Health endpoint: `curl http://localhost:5000/api/timeseries/health`
3. Backend logs: `docker-compose -f docker-compose.prod.yml logs backend`

**Fix**: Usually resolved by restarting backend

### Backend Can't Connect to ClickHouse

**Symptoms**: "Connection refused" in backend logs

**Check**:
1. ClickHouse is healthy: `docker-compose -f docker-compose.prod.yml ps clickhouse`
2. Credentials in `.env` match ClickHouse config
3. Network connectivity: `docker-compose -f docker-compose.prod.yml exec backend ping clickhouse`

**Fix**:
```bash
# Restart ClickHouse
docker-compose -f docker-compose.prod.yml restart clickhouse

# Wait 30 seconds, then restart backend
sleep 30
docker-compose -f docker-compose.prod.yml restart backend
```

### Out of Memory

**Symptoms**: Containers restarting, OOM errors in logs

**Fix**: Add memory limits in docker-compose.prod.yml and increase host memory

### Port Already in Use

**Symptoms**: "port is already allocated" error

**Fix**: Change ports in `.env`:
```bash
FRONTEND_PORT=8080
BACKEND_PORT=5001
```

---

## 📦 Using Pre-built Images

If you have images in a registry:

1. Edit `docker-compose.prod.yml`:
```yaml
backend:
  image: myregistry.azurecr.io/ts-analytics-backend:v1.0.0
  # Remove 'build:' section

frontend:
  image: myregistry.azurecr.io/ts-analytics-frontend:v1.0.0
  # Remove 'build:' section
```

2. Deploy:
```bash
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d
```

---

## 🔄 CI/CD Integration

### GitHub Actions Example

```yaml
name: Deploy

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Deploy to server
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.HOST }}
          username: ${{ secrets.USERNAME }}
          key: ${{ secrets.SSH_KEY }}
          script: |
            cd /opt/timeseries-analytics
            git pull
            docker-compose -f docker-compose.prod.yml up -d --build
```

### Jenkins Pipeline

```groovy
pipeline {
    agent any
    stages {
        stage('Deploy') {
            steps {
                sh '''
                    cd /opt/timeseries-analytics
                    docker-compose -f docker-compose.prod.yml pull
                    docker-compose -f docker-compose.prod.yml up -d
                '''
            }
        }
    }
}
```

---

## 📞 Support

### Get Help
- Check logs first: `docker-compose logs -f`
- Review PRODUCTION-DEPLOYMENT.md for detailed info
- Check health endpoints

### Report Issues
Include:
1. Output of `docker-compose ps`
2. Relevant logs from `docker-compose logs`
3. Environment (.env) - **REDACT PASSWORDS**
4. Steps to reproduce

---

## 📝 Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ASPNET_ENVIRONMENT` | ASP.NET environment | Production | Yes |
| `FRONTEND_PORT` | Frontend port | 80 | No |
| `BACKEND_PORT` | Backend port | 5000 | No |
| `CLICKHOUSE_USER` | Database user | demo_user | Yes |
| `CLICKHOUSE_PASSWORD` | Database password | demo_pass | **YES - CHANGE IN PROD** |

---

## ✅ Summary

**What to do**:
1. Copy repo to server
2. Edit `.env` file (change password!)
3. Run `docker-compose -f docker-compose.prod.yml up -d --build`
4. Verify at http://server-ip

**What it does**:
- Starts 3 containers (frontend, backend, database)
- Frontend proxies API calls to backend
- All services auto-restart on failure

**Production considerations**:
- Add SSL/TLS certificate
- Change default passwords
- Set up monitoring
- Configure backups
- Set resource limits

That's it! The application is fully self-contained and ready for production.
