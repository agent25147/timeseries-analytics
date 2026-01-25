# TimeSeriesAnalytics

Modern OLAP analytics platform with ClickHouse, .NET 8, and Angular 18.

## ✨ Features

- **Server-side AG-Grid Enterprise**: Filtering, sorting, grouping, row grouping, and pivoting
- **ClickHouse Integration**: High-performance time-series data storage and querying
- **Real-time Analytics**: Process millions of records with sub-second response times
- **Dockerized Deployment**: One-command deployment with Docker Compose
- **Production Ready**: Health checks, monitoring, and scalable architecture

---

## 🚀 Quick Start

### For Developers (Local Development)

```bash
# Start all services
docker-compose up --build -d

# View logs
docker-compose logs -f
```

**Access:**
- Frontend: http://localhost:4200
- Backend API: http://localhost:5000/swagger
- ClickHouse: http://localhost:8123

### For DevOps (Production Deployment)

See **[DEVOPS-README.md](./DEVOPS-README.md)** for complete deployment guide.

**TL;DR:**
```bash
# 1. Configure environment
cp .env.example .env
nano .env  # Change password!

# 2. Deploy
docker-compose -f docker-compose.prod.yml up -d --build

# 3. Verify
curl http://localhost/api/timeseries/health
```

---

## 📁 Project Structure

```
timeseries-analytics/
├── backend/                    # .NET 8 Web API
│   ├── TimeSeriesAnalytics.Api/
│   │   ├── Controllers/       # API endpoints
│   │   ├── Services/          # ClickHouse & query services
│   │   └── Models/            # DTOs and domain models
│   └── Dockerfile
│
├── frontend/                   # Angular 18 + AG Grid
│   ├── timeseries-analytics-ui/
│   │   ├── src/
│   │   │   ├── app/
│   │   │   │   ├── components/  # Data grid component
│   │   │   │   └── services/    # API service
│   │   │   └── environments/    # Environment configs
│   ├── nginx.conf             # Nginx reverse proxy
│   └── Dockerfile
│
├── clickhouse/                # Database configuration
│   └── config/
│       └── users.xml
│
├── docker-compose.yml         # Development deployment
├── docker-compose.prod.yml    # Production deployment
└── .env.example               # Environment variables template
```

---

## 🏗️ Architecture

### Request Flow

```
User Browser
    ↓
Frontend (Nginx :80)
    ├─→ /           → Angular App (Static Files)
    └─→ /api/*      → Proxy to Backend
            ↓
    Backend (.NET :8080)
            ↓
    ClickHouse (:8123)
```

### Key Design Decisions

1. **Nginx Reverse Proxy**: Frontend uses relative path `/api` which Nginx proxies to backend
   - ✅ No CORS issues (same origin)
   - ✅ Works in any environment
   - ✅ No hardcoded URLs

2. **Containerized Services**: All services run in Docker containers
   - ✅ Consistent environments
   - ✅ Easy scaling
   - ✅ Simplified deployment

3. **ClickHouse for Analytics**: OLAP database optimized for time-series data
   - ✅ Column-oriented storage
   - ✅ Excellent compression
   - ✅ Fast aggregations

---

## 🛠️ Development

### Prerequisites

- Docker & Docker Compose
- .NET 8 SDK (for local backend development)
- Node.js 22+ (for local frontend development)

### Local Development Setup

#### Backend
```bash
cd backend/TimeSeriesAnalytics.Api
dotnet restore
dotnet run
```

#### Frontend
```bash
cd frontend/timeseries-analytics-ui
npm install
npm start
```

See detailed guides:
- [Backend Development](./backend/README.md)
- [Frontend Development](./frontend/README.md)

---

## 📚 Documentation

- **[DEVOPS-README.md](./DEVOPS-README.md)** - Quick deployment guide for DevOps
- **[PRODUCTION-DEPLOYMENT.md](./PRODUCTION-DEPLOYMENT.md)** - Comprehensive production deployment guide
- **[CONFIGURATION.md](./CONFIGURATION.md)** - Environment configuration reference
- **[DEPLOYMENT.md](./DEPLOYMENT.md)** - Detailed deployment scenarios

---

## 🔧 Configuration

### Environment Variables

Create `.env` from template:
```bash
cp .env.example .env
```

**Key variables:**
- `CLICKHOUSE_PASSWORD` - Database password (**change in production!**)
- `FRONTEND_PORT` - Frontend port (default: 80)
- `BACKEND_PORT` - Backend API port (default: 5000)
- `ASPNET_ENVIRONMENT` - ASP.NET environment (Development/Production)

### API Endpoints

The frontend automatically calls the correct API based on environment:

- **Development**: `https://localhost:7129/api` (local .NET dev server)
- **Docker/Production**: `/api` (proxied through Nginx)

---

## 🧪 Testing

### Seed Sample Data
```bash
# Generate 10 million records
curl -X POST http://localhost:5000/api/dataseed/seed \
  -H "Content-Type: application/json" \
  -d '{"recordCount": 10000000, "batchSize": 100000}'
```

### Health Check
```bash
curl http://localhost:5000/api/timeseries/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-24T10:30:00Z",
  "database": "connected"
}
```

---

## 📊 AG Grid Features

The application demonstrates advanced AG Grid Enterprise features:

- **Server-Side Row Model**: Handles millions of rows efficiently
- **Row Grouping**: Group by dimensions (metric, deviceType, etc.)
- **Pivoting**: Cross-tabulate data with pivot mode
- **Aggregation**: Sum, avg, min, max on value columns
- **Filtering**: Advanced filters on all columns
- **Sorting**: Multi-column sorting
- **Infinite Scroll**: Load data on-demand

---

## 🚀 Deployment Options

### 1. Docker Compose (Simplest)
```bash
docker-compose -f docker-compose.prod.yml up -d --build
```

### 2. Pre-built Images
```bash
# Build and push
./build-images.bat v1.0.0 myregistry.azurecr.io

# On server: pull and run
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d
```

### 3. Kubernetes
See [PRODUCTION-DEPLOYMENT.md](./PRODUCTION-DEPLOYMENT.md) for Kubernetes manifests.

### 4. Cloud Platforms
- AWS ECS/Fargate
- Azure Container Instances
- Google Cloud Run

See [PRODUCTION-DEPLOYMENT.md](./PRODUCTION-DEPLOYMENT.md) for cloud deployment guides.

---

## 🔍 Monitoring

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f backend
```

### Service Health
```bash
docker-compose ps
```

### Metrics
- Frontend: Nginx access logs
- Backend: ASP.NET Core logging
- Database: ClickHouse system tables

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test with Docker Compose
5. Submit a pull request

---

## 📄 License

MIT License - See LICENSE file for details

---

## 🆘 Support

### Common Issues

**Frontend shows "Cannot connect to API"**
- Check backend is running: `docker-compose ps`
- Check logs: `docker-compose logs backend`
- Verify health: `curl http://localhost:5000/api/timeseries/health`

**ClickHouse connection failed**
- Wait 30 seconds after startup
- Check credentials in `.env`
- Verify ClickHouse is healthy: `curl http://localhost:8123/ping`

**Out of memory**
- Reduce batch size when seeding data
- Increase Docker memory limits
- Add resource limits in docker-compose

For more troubleshooting, see [DEVOPS-README.md](./DEVOPS-README.md)

---

## 🎯 Tech Stack

- **Frontend**: Angular 18, AG Grid Enterprise, TypeScript
- **Backend**: .NET 8, ASP.NET Core, Dapper
- **Database**: ClickHouse (OLAP)
- **Infrastructure**: Docker, Nginx, Docker Compose
- **Development**: Visual Studio Code, Cursor

---

## 📈 Performance

- **Query Speed**: Sub-second response for 10M+ records
- **Data Ingestion**: 100K+ records/second
- **Compression**: ~10x with ClickHouse
- **Scalability**: Horizontal scaling via container replication

---

## 🔐 Security

Production checklist:
- ✅ Change default ClickHouse password
- ✅ Use HTTPS/SSL certificates
- ✅ Configure firewall rules
- ✅ Enable authentication/authorization
- ✅ Regular security updates
- ✅ Secure environment variables

See [PRODUCTION-DEPLOYMENT.md](./PRODUCTION-DEPLOYMENT.md) for complete security guide.

---

**Ready to deploy?** See [DEVOPS-README.md](./DEVOPS-README.md) for the 5-minute deployment guide! 🚀
