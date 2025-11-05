# Docker Deployment Guide

## Overview

This guide explains how to deploy the Halulu API using Docker. The Docker setup includes:
- **PostgreSQL** database (version 15-alpine)
- **Halulu API** (.NET 8 ASP.NET Core application)
- **Network** for inter-container communication

---

## Prerequisites

- Docker installed and running
- Docker Compose installed (usually comes with Docker Desktop)
- Port 5000 available (API)
- Port 5432 available (PostgreSQL)

---

## Quick Start

### 1. Build and Start Services

```bash
docker-compose up -d
```

This command:
- ✅ Builds the API image from the Dockerfile
- ✅ Starts PostgreSQL container
- ✅ Starts the API container
- ✅ Runs database migrations automatically
- ✅ Connects both containers via custom network

### 2. Verify Services are Running

```bash
docker-compose ps
```

Expected output:
```
NAME              STATUS
halulu-postgres   Up (healthy)
halulu-api        Up
```

### 3. Access the API

**From your machine:**
```
http://localhost:5000/api/health/test
```

**Response:**
```json
{
  "success": true,
  "message": "API is accessible and working correctly",
  "timestamp": "2024-10-30T14:05:23.456Z"
}
```

**API Documentation (Swagger):**
```
http://localhost:5000/swagger
```

---

## Docker Compose Configuration

### Key Components

#### Postgres Service
```yaml
postgres:
  image: postgres:15-alpine
  container_name: halulu-postgres
  environment:
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres
    POSTGRES_DB: halulu_db_dev
  ports:
    - "5432:5432"
  volumes:
    - postgres_data:/var/lib/postgresql/data
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres"]
    interval: 10s
    timeout: 5s
    retries: 5
  networks:
    - halulu-network
```

**Details:**
- Uses Alpine Linux for smaller image size
- Database name: `halulu_db_dev`
- Default credentials: postgres/postgres
- Health check ensures container is ready before API starts
- Persists data in volume

#### API Service
```yaml
api:
  build: .
  container_name: halulu-api
  environment:
    ASPNETCORE_ENVIRONMENT: Development
    ASPNETCORE_URLS: http://0.0.0.0:5000
    ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=halulu_db_dev;Username=postgres;Password=postgres
  ports:
    - "5000:5000"
  depends_on:
    postgres:
      condition: service_healthy
  networks:
    - halulu-network
```

**Details:**
- Builds from Dockerfile in project root
- Listens on `0.0.0.0:5000` (all IPv4 addresses) - **Critical for Docker**
- Waits for PostgreSQL to be healthy before starting
- Uses internal DNS name `postgres` to connect to database

---

## IPv4 Binding (The Fix)

### What Was the Problem?

The original setup had the app listening on IPv6 only (`[::]`), which made it inaccessible from your local machine. The fix involved two changes:

### Solution 1: Kestrel Configuration (Program.cs)

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // Listen on all IPv4 addresses on port 5000
});
```

This explicitly tells Kestrel to:
- ✅ Listen on all available IPv4 addresses (`0.0.0.0`)
- ✅ Listen on port 5000
- ✅ Accept connections from anywhere (Docker container, localhost, remote hosts)

### Solution 2: Environment Variable (docker-compose.yml)

```yaml
environment:
  ASPNETCORE_URLS: http://0.0.0.0:5000
```

This doubles down on the IPv4 binding by explicitly setting the URL in the container.

---

## Common Issues and Solutions

### Issue 1: "Connection refused" Error

**Problem:** `http://localhost:5000` returns "connection refused"

**Solutions:**

1. **Check if container is running:**
   ```bash
   docker-compose ps
   ```
   If API shows "exited", check logs:
   ```bash
   docker-compose logs api
   ```

2. **Wait for PostgreSQL to be healthy:**
   - API starts only AFTER PostgreSQL reports healthy
   - This takes ~10-15 seconds on first run

3. **Check port availability:**
   ```bash
   # Windows
   netstat -ano | findstr :5000
   
   # Linux/Mac
   lsof -i :5000
   ```
   Kill any process on port 5000 if needed

### Issue 2: Database Connection Fails

**Problem:** API starts but can't connect to database

**Solution:** Verify PostgreSQL is healthy:
```bash
docker-compose logs postgres
```

Should show:
```
postgres | ready to accept connections
```

### Issue 3: API Not Accessible from Remote Host

**Problem:** Can access from localhost but not from `192.168.1.39:5000`

**Reason:** Docker port mapping only works for localhost by default

**Solution:** Change docker-compose.yml:
```yaml
ports:
  - "0.0.0.0:5000:5000"  # Listen on all interfaces
```

Then restart:
```bash
docker-compose down
docker-compose up -d
```

### Issue 4: Migrations Not Running

**Problem:** Database tables don't exist

**Reason:** Migrations run automatically on app startup, but may fail if database isn't ready

**Solution:** 
1. Check API logs for migration errors:
   ```bash
   docker-compose logs api
   ```

2. Manually run migrations:
   ```bash
   docker-compose exec api dotnet ef database update
   ```

---

## Useful Docker Commands

### View Logs

**All services:**
```bash
docker-compose logs
```

**Specific service:**
```bash
docker-compose logs -f api      # Follow logs
docker-compose logs postgres    # View only postgres logs
```

### Execute Commands

**Inside API container:**
```bash
docker-compose exec api bash
docker-compose exec api dotnet ef database update
```

**Inside PostgreSQL:**
```bash
docker-compose exec postgres psql -U postgres -d halulu_db_dev
```

### Stop Services

```bash
docker-compose down          # Stop and remove containers
docker-compose down -v       # Remove containers and volumes (deletes data!)
docker-compose stop          # Stop without removing
```

### Rebuild Image

```bash
docker-compose up --build    # Rebuild if you changed Dockerfile or code
```

### View Resource Usage

```bash
docker stats
```

---

## Database Access

### From Inside Docker

```bash
docker-compose exec postgres psql -U postgres -d halulu_db_dev
```

### From Your Machine

Use any PostgreSQL client:
- Connection string: `postgresql://postgres:postgres@localhost:5432/halulu_db_dev`
- Host: `localhost`
- Port: `5432`
- Database: `halulu_db_dev`
- Username: `postgres`
- Password: `postgres`

---

## API Endpoints

### Health Check (Verify Access)

```bash
curl http://localhost:5000/api/health/test
```

### Authentication Example

**Send OTP:**
```bash
curl -X POST http://localhost:5000/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com"}'
```

**Verify OTP:**
```bash
curl -X POST http://localhost:5000/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "otpCode": "123456"}'
```

### Swagger Documentation

Browse to:
```
http://localhost:5000/swagger
```

---

## Environment Variables

You can customize deployment by editing docker-compose.yml:

```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Development  # or Production
  ASPNETCORE_URLS: http://0.0.0.0:5000
  ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;...
```

---

## Persistence

### Data Persistence

PostgreSQL data is stored in a Docker volume:
```yaml
volumes:
  postgres_data:
```

Data persists across container restarts:
```bash
docker-compose down     # Stop containers
docker-compose up       # Start again - data still there!
```

To delete data:
```bash
docker-compose down -v  # -v removes volumes
```

---

## Network Architecture

```
┌─────────────────────────────────────┐
│   halulu-network (bridge)           │
├─────────────────────────────────────┤
│                                     │
│  ┌──────────────┐    ┌──────────┐  │
│  │ halulu-api   │───▶│ postgres │  │
│  │ :5000        │    │ :5432    │  │
│  └──────────────┘    └──────────┘  │
│                                     │
└─────────────────────────────────────┘
          ▲
          │ Port Mapping
          │
    ┌─────┴─────┐
    │   Your    │
    │  Machine  │
    └───────────┘
```

**Key Points:**
- API and PostgreSQL communicate via container DNS (`postgres`)
- Your machine accesses API via port mapping (localhost:5000)
- Network isolation improves security

---

## Troubleshooting Checklist

- [ ] Docker daemon is running
- [ ] No other service on port 5000 or 5432
- [ ] `docker-compose up -d` completed without errors
- [ ] `docker-compose ps` shows both services as healthy/up
- [ ] `http://localhost:5000/api/health/test` returns 200 OK
- [ ] Check logs: `docker-compose logs api`
- [ ] PostgreSQL is healthy: `docker-compose logs postgres`
- [ ] Database migrations ran: Check app logs during startup

---

## Production Deployment

For production deployment:

1. **Change environment:**
   ```yaml
   environment:
     ASPNETCORE_ENVIRONMENT: Production
   ```

2. **Use strong passwords:**
   ```yaml
   environment:
     POSTGRES_PASSWORD: <very-strong-password>
   ```

3. **Change database name:**
   ```yaml
   environment:
     POSTGRES_DB: halulu_db_prod
   ```

4. **Update connection string**

5. **Use external database** (optional):
   ```yaml
   # Remove postgres service, use external RDS/Azure Database
   environment:
     ConnectionStrings__DefaultConnection: Host=external-host;...
   ```

6. **Enable HTTPS** in Program.cs

7. **Use proper secrets management** (not in docker-compose.yml)

---

## Support & Debugging

### Enable Detailed Logging

Check `appsettings.json` for logging configuration

### View Startup Sequence

```bash
docker-compose logs api | grep -i "migrat\|listen\|error"
```

### Verify Network Connectivity

```bash
docker-compose exec api ping postgres
```

---

## References

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)
- [ASP.NET Core Docker Documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)