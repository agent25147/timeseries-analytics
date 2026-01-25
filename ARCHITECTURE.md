# Architecture & URL Flow

## 🏗️ How API Calls Work (The Magic Explained)

### Development (Local Machine)
```
┌─────────────────────────────────────────┐
│  Developer's Machine                    │
│                                         │
│  Angular Dev Server (ng serve)          │
│  http://localhost:4200                  │
│  apiUrl: 'https://localhost:7129/api'   │
│           └──────────────┐              │
│                          ↓              │
│  .NET Dev Server (dotnet run)           │
│  https://localhost:7129                 │
│  (with HTTPS & Swagger)                 │
│           └──────────────┐              │
│                          ↓              │
│  ClickHouse (Docker)                    │
│  http://localhost:8123                  │
└─────────────────────────────────────────┘
```

### Docker (Local Testing)
```
┌──────────────────────────────────────────────────┐
│  Docker Network: ts-network                      │
│                                                  │
│  ┌────────────────────────────────────────┐    │
│  │ Frontend Container (nginx)             │    │
│  │ Port: 4200 → 80                        │    │
│  │                                        │    │
│  │ When user requests:                    │    │
│  │   http://localhost:4200/              │    │
│  │   → Serves Angular app                │    │
│  │                                        │    │
│  │   http://localhost:4200/api/query     │    │
│  │   → Proxies to backend container      │    │
│  └────────────┬───────────────────────────┘    │
│               │                                 │
│               ↓ (proxy_pass to backend:8080)   │
│  ┌────────────────────────────────────────┐    │
│  │ Backend Container (.NET)               │    │
│  │ Internal: 8080                         │    │
│  │ External: 5000 → 8080                  │    │
│  └────────────┬───────────────────────────┘    │
│               │                                 │
│               ↓                                 │
│  ┌────────────────────────────────────────┐    │
│  │ ClickHouse Container                   │    │
│  │ Port: 8123, 9000                       │    │
│  └────────────────────────────────────────┘    │
└──────────────────────────────────────────────────┘
```

### Production (Any Domain)
```
┌────────────────────────────────────────────────────┐
│  Internet                                          │
│  User visits: https://analytics.company.com        │
└────────────────┬───────────────────────────────────┘
                 │
                 ↓ DNS resolves to server IP
┌────────────────────────────────────────────────────┐
│  Production Server                                 │
│                                                    │
│  ┌──────────────────────────────────────────┐    │
│  │  Frontend Container (Nginx)              │    │
│  │  Port: 80 (or 443 with SSL)              │    │
│  │                                          │    │
│  │  nginx.conf:                             │    │
│  │  ┌────────────────────────────────────┐ │    │
│  │  │ location / {                       │ │    │
│  │  │   # Serve Angular static files     │ │    │
│  │  │   try_files $uri /index.html;      │ │    │
│  │  │ }                                  │ │    │
│  │  │                                    │ │    │
│  │  │ location /api/ {                   │ │    │
│  │  │   # Proxy to backend container     │ │    │
│  │  │   proxy_pass http://backend:8080;  │ │    │
│  │  │ }                                  │ │    │
│  │  └────────────────────────────────────┘ │    │
│  └──────────┬───────────────────────────────┘    │
│             │                                     │
│             ↓ Internal Docker network             │
│  ┌──────────────────────────────────────────┐    │
│  │  Backend Container                       │    │
│  │  Hostname: backend (internal)            │    │
│  │  Port: 8080 (not exposed to internet)    │    │
│  └──────────┬───────────────────────────────┘    │
│             │                                     │
│             ↓                                     │
│  ┌──────────────────────────────────────────┐    │
│  │  ClickHouse Container                    │    │
│  │  Hostname: clickhouse (internal)         │    │
│  │  Port: 8123 (not exposed to internet)    │    │
│  └──────────────────────────────────────────┘    │
└────────────────────────────────────────────────────┘
```

## 🔍 Request Flow Example

### User Action: "Load Data Grid"

#### Step 1: Angular Makes API Call
```typescript
// In Angular service (timeseries.service.ts)
constructor(private http: HttpClient) {
  this.apiUrl = environment.apiUrl;  // '/api' in production
}

getData() {
  // Makes call to: https://analytics.company.com/api/timeseries/query
  return this.http.post(`${this.apiUrl}/timeseries/query`, request);
}
```

#### Step 2: Nginx Receives Request
```
Incoming: https://analytics.company.com/api/timeseries/query

Nginx checks location blocks:
  - Does NOT match: location /
  - DOES match: location /api/
  
→ Proxy to: http://backend:8080/api/timeseries/query
```

#### Step 3: Backend Processes Request
```
Backend receives: POST /api/timeseries/query
  ↓
TimeSeriesController.Query()
  ↓
QueryBuilderService builds SQL
  ↓
ClickHouseService executes query
  ↓
Return JSON response
```

#### Step 4: Response Flows Back
```
Backend → Nginx → User's Browser → Angular renders in AG Grid
```

## 🎯 Why This Architecture?

### ✅ Benefits

1. **No Hardcoded URLs**
   - Frontend uses relative path `/api`
   - Works on any domain without code changes

2. **No CORS Issues**
   - Same origin (both from analytics.company.com)
   - Browser doesn't block requests

3. **Security**
   - Backend not directly exposed to internet
   - Only Nginx (frontend) has public port
   - Internal Docker network for backend/database

4. **Flexibility**
   - Easy to add SSL at Nginx level
   - Can add authentication at proxy level
   - Can rate limit API calls at Nginx

5. **Scalability**
   - Can run multiple backend containers
   - Nginx load balances automatically
   - Add caching at Nginx level

### ❌ Without Proxy (Problems)

```
Frontend: https://app.company.com
Backend:  https://api.company.com  ← Different origin!

Problems:
  ❌ CORS configuration needed
  ❌ Two domains to manage
  ❌ Two SSL certificates
  ❌ CORS pre-flight requests (slower)
  ❌ Hardcoded backend URL in frontend
```

## 🔐 Security Layers

```
Internet (Untrusted)
    ↓
Firewall (Only allow 80/443)
    ↓
Nginx (Frontend Container)
    ├─→ Serve static files (public)
    └─→ Proxy /api/* (authenticated in future)
            ↓
Docker Network (Internal, Isolated)
    ↓
Backend Container (Not accessible from internet)
    ↓
ClickHouse Container (Not accessible from internet)
```

## 📊 Environment Variables Flow

### How API URL is Set

```typescript
// environment.development.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7129/api'  ← Points to local .NET
};

// environment.prod.ts
export const environment = {
  production: true,
  apiUrl: '/api'  ← Relative path, proxied by Nginx
};
```

### Angular Build Process
```bash
# Development build
ng serve
→ Uses environment.development.ts
→ Calls https://localhost:7129/api

# Production build
ng build --configuration production
→ Uses environment.prod.ts (via angular.json fileReplacements)
→ Calls /api (wherever the app is hosted)
```

## 🌍 Multiple Environment Support

### Same Code, Different Environments

```
┌─────────────────────┬──────────────────┬────────────────┐
│ Environment         │ User Accesses    │ API Calls Go To│
├─────────────────────┼──────────────────┼────────────────┤
│ Local Dev           │ localhost:4200   │ localhost:7129 │
│ Docker Local        │ localhost:4200   │ /api → :5000   │
│ Staging             │ staging.co.com   │ /api → backend │
│ Production          │ analytics.co.com │ /api → backend │
└─────────────────────┴──────────────────┴────────────────┘
```

**Key Point**: Production and staging use the SAME Docker images!
No code changes needed for different environments.

## 🚀 Summary

**The Magic**: 
- Frontend always calls `/api`
- Nginx sees `/api` and proxies to backend
- Backend and database are internal only
- Works on any domain with zero configuration

**Developer Experience**:
- Local dev: ng serve + dotnet run (both visible)
- Docker: docker-compose up (all containerized)
- Production: Same as Docker, just add domain

**No Code Changes Between Environments!** ✨
