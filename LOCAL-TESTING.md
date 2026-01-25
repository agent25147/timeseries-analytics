# Local Testing Guide

## 🧪 Test Production Setup on Local Machine

### Step 1: Create .env file

```bash
# Copy the example
cp .env.example .env
```

Or manually create `.env` file with these contents:

```env
# Application Settings
ASPNET_ENVIRONMENT=Production
FRONTEND_PORT=4200
BACKEND_PORT=5000

# ClickHouse Configuration
CLICKHOUSE_USER=demo_user
CLICKHOUSE_PASSWORD=my_secure_password_123
```

### Step 2: Deploy Locally

```bash
# Build and start all services
docker-compose -f docker-compose.prod.yml up -d --build

# Watch the logs
docker-compose -f docker-compose.prod.yml logs -f
```

### Step 3: Access the Application

Since DevOps will expose a port and you'll access via IP, test the same way:

**Access URLs (replace `localhost` with server IP in production):**
- Frontend: http://localhost:4200
- Backend API: http://localhost:5000/swagger
- Health Check: http://localhost:5000/api/timeseries/health

**Or if you want to test on port 80 (like production):**

Change `.env` to:
```env
FRONTEND_PORT=80
```

Then access: http://localhost

### Step 4: Test from Another Device (Simulating Production)

1. Find your local IP address:
   ```bash
   # Windows
   ipconfig
   # Look for IPv4 Address (e.g., 192.168.1.100)
   ```

2. From another device on same network:
   - Frontend: http://192.168.1.100:4200
   - This simulates how it will work when DevOps deploys!

### Step 5: Verify Everything Works

```bash
# Check all containers are running
docker-compose -f docker-compose.prod.yml ps

# Test health endpoint
curl http://localhost:5000/api/timeseries/health

# Seed some data
curl -X POST http://localhost:5000/api/dataseed/seed \
  -H "Content-Type: application/json" \
  -d "{\"recordCount\": 10000, \"batchSize\": 1000}"
```

### Step 6: Stop and Clean Up

```bash
# Stop all services
docker-compose -f docker-compose.prod.yml down

# Remove volumes (if you want fresh start)
docker-compose -f docker-compose.prod.yml down -v
```

## 📊 How Environment Variables Flow

```
.env file (your local file)
    ↓
CLICKHOUSE_PASSWORD=my_secure_password_123
    ↓
docker-compose.prod.yml reads it
    ↓
${CLICKHOUSE_PASSWORD:-demo_pass}
    ↓
Uses "my_secure_password_123" (from .env)
    ↓
Passes to ClickHouse container
    ↓
ClickHouse uses that password
```

## 🔐 Security Note

**Never commit `.env` to Git!**

The `.gitignore` should already have `.env` listed.

Check:
```bash
cat .gitignore | grep .env
```

## 🌐 Production Deployment (IP Address Access)

When DevOps deploys to a server with IP `45.123.45.67`:

**Users will access:**
- Frontend: http://45.123.45.67
- Backend API: http://45.123.45.67:5000/swagger

**Frontend will call:**
- `/api/timeseries/query` → Proxied by Nginx to backend

**Everything works the same!** No code changes needed.

## ✅ What This Test Proves

- ✅ Docker images build correctly
- ✅ All services start and connect
- ✅ Frontend proxies API calls to backend
- ✅ ClickHouse database initializes
- ✅ Health checks work
- ✅ Application accessible via IP address
- ✅ Same setup DevOps will use

## 🎯 For DevOps (Their Setup)

They will do the exact same thing on their server:

1. Copy repository to server
2. Create `.env` file (from .env.example)
3. Run: `docker-compose -f docker-compose.prod.yml up -d --build`
4. Access via server IP

**No domain needed!** Works perfectly with just IP address.
