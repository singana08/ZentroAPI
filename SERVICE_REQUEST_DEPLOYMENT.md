# Service Request System - Deployment & Setup Guide

## Overview

The Service Request Booking System has been fully implemented and is ready for deployment. This guide provides step-by-step instructions for deploying the system to your environment.

## Pre-Deployment Checklist

- [x] All code files created and reviewed
- [x] Database migration generated
- [x] Service registered in dependency injection
- [x] Build succeeds with zero errors
- [x] Documentation complete
- [x] Security implementation validated
- [x] Error handling implemented

## System Requirements

### Software
- .NET 8.0 or higher
- PostgreSQL 12 or higher
- Docker (optional, for containerized deployment)

### Hardware (Minimum)
- 1GB RAM
- 500MB disk space
- Network connectivity

## Installation & Deployment

### Step 1: Pull Latest Code

```bash
cd d:\KalyaniMatrimony\Git\HaluluAPI
git pull origin main  # or your branch
```

### Step 2: Build Project

```bash
dotnet build
```

**Expected Output**:
```
Build succeeded in X.Xs
```

### Step 3: Apply Database Migration

```bash
# Apply the new migration
dotnet ef database update

# Or specify migration
dotnet ef database update AddServiceRequestTable
```

**Verify** the `service_requests` table was created:
```sql
SELECT * FROM halulu_api.service_requests LIMIT 1;
```

### Step 4: Run Application

#### Development
```bash
dotnet run
```

#### Production
```bash
dotnet publish -c Release
cd bin/Release/net8.0/publish
dotnet HaluluAPI.dll
```

### Step 5: Verify API

Access Swagger UI:
```
http://localhost:5000/swagger  # or configured port
```

Look for these new endpoints:
- POST /api/service-request
- PUT /api/service-request/{id}
- GET /api/service-request/{id}
- GET /api/service-request/user/{userId}
- DELETE /api/service-request/{id}/cancel
- GET /api/service-request/admin/all

## Docker Deployment

### Build Docker Image

```bash
docker build -t halulu-api:latest .
```

### Run Docker Container

```bash
docker run -d \
  --name halulu-api \
  -p 8000:8000 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=halulu_api;Username=postgres;Password=your_password" \
  halulu-api:latest
```

### Docker Compose

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: halulu_api
      POSTGRES_PASSWORD: your_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  halulu-api:
    build: .
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=halulu_api;Username=postgres;Password=your_password"
    ports:
      - "8000:8000"
    depends_on:
      - postgres

volumes:
  postgres_data:
```

Deploy with:
```bash
docker-compose up -d
```

## Configuration

### appsettings.json

Ensure these settings are configured:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=halulu_api;Username=postgres;Password=your_password"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-characters",
    "Issuer": "HaluluAPI",
    "Audience": "HaluluMobileApp"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001",
      "https://your-domain.com"
    ]
  }
}
```

### Environment Variables

```bash
# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=halulu_api;Username=postgres;Password=your_password"

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=halulu_api;Username=postgres;Password=your_password"
```

## Verification Steps

### 1. Database Connection

```sql
-- Connect to PostgreSQL
psql -U postgres -d halulu_api -h localhost

-- Verify table exists
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'halulu_api' AND table_name = 'service_requests';

-- Verify columns
\d halulu_api.service_requests

-- Verify indexes
SELECT indexname FROM pg_indexes 
WHERE tablename = 'service_requests';
```

### 2. API Health Check

```bash
curl -X GET http://localhost:5000/api/health/ping \
  -H "Content-Type: application/json"
```

### 3. Test Authentication

```bash
# Send OTP
curl -X POST http://localhost:5000/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com"}'

# Verify OTP and get token
curl -X POST http://localhost:5000/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "otpCode": "123456"
  }'
```

### 4. Test Service Request Endpoints

```bash
# Create request (replace TOKEN with actual JWT token)
curl -X POST http://localhost:5000/api/service-request \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bookingType": "book_now",
    "mainCategory": "Cleaning",
    "subCategory": "Deep Cleaning",
    "date": "2024-11-01",
    "location": "123 Main St"
  }'

# Get user requests
curl -X GET "http://localhost:5000/api/service-request/user/USER_ID" \
  -H "Authorization: Bearer TOKEN"
```

## Monitoring & Logs

### View Application Logs

```bash
# Real-time logs
dotnet run 2>&1 | Tee-Object -FilePath logs.txt

# Or run in background and check logs
tail -f logs.txt
```

### Database Logs

```sql
-- Check for errors
SELECT * FROM pg_stat_statements 
ORDER BY mean_time DESC 
LIMIT 10;

-- Monitor connections
SELECT datname, count(*) FROM pg_stat_activity GROUP BY datname;
```

### Swagger API Testing

1. Open: `http://localhost:5000/swagger`
2. Click "Authorize" and enter your JWT token
3. Try endpoints directly from UI

## Troubleshooting

### Issue: Migration Fails

```
Error: Connection refused
```

**Solution**: Ensure PostgreSQL is running
```bash
# Check PostgreSQL
psql -U postgres -c "SELECT version();"

# Or start PostgreSQL service
sudo systemctl start postgresql  # Linux
brew services start postgresql  # Mac
```

### Issue: JWT Token Error

```
Error: "Invalid token" or "Unauthorized"
```

**Solution**: 
- Verify token format: `Authorization: Bearer <token>`
- Check token expiration
- Verify JWT settings in appsettings.json

### Issue: CORS Error

```
Access to XMLHttpRequest blocked by CORS policy
```

**Solution**: Add your frontend URL to `Cors.AllowedOrigins` in appsettings.json

### Issue: Database Locked

```
Error: database is locked
```

**Solution**:
```bash
# Kill conflicting processes
lsof -i :5432  # Linux/Mac

# Or restart PostgreSQL
sudo systemctl restart postgresql
```

### Issue: Port Already in Use

```
Error: Address already in use
```

**Solution**:
```bash
# Find process using port
lsof -i :5000  # Linux/Mac
netstat -ano | findstr :5000  # Windows

# Kill process
kill -9 PID  # Linux/Mac
taskkill /PID <PID> /F  # Windows
```

## Performance Tuning

### Database Optimization

```sql
-- Analyze tables for query planning
ANALYZE service_requests;

-- Check index usage
SELECT * FROM pg_stat_user_indexes;

-- Monitor slow queries
ALTER SYSTEM SET log_min_duration_statement = 1000;  -- Log queries > 1s
SELECT pg_reload_conf();
```

### Connection Pooling

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=halulu_api;Username=postgres;Password=your_password;Maximum Pool Size=20;Minimum Pool Size=5;"
}
```

### Caching (Future)

```csharp
// Add Redis caching in Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

## Security Hardening

### 1. Update appsettings.json

```bash
# Generate strong secret key
openssl rand -base64 32
```

Set in `JwtSettings.SecretKey`

### 2. Enable HTTPS

```json
"HttpsRedirection": {
  "Enabled": true,
  "HttpPort": 5000,
  "HttpsPort": 5001
}
```

### 3. Add Security Headers

```csharp
// In Program.cs
app.UseHsts();
app.UseXContentTypeOptions();
```

### 4. Database User Permissions

```sql
-- Create limited database user
CREATE USER api_user WITH PASSWORD 'strong_password';
GRANT CONNECT ON DATABASE halulu_api TO api_user;
GRANT USAGE ON SCHEMA halulu_api TO api_user;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA halulu_api TO api_user;
```

## Backup & Recovery

### Backup Database

```bash
# Full backup
pg_dump -U postgres halulu_api > halulu_api_backup.sql

# Compressed backup
pg_dump -U postgres -Fc halulu_api > halulu_api_backup.dump
```

### Restore Database

```bash
# From SQL dump
psql -U postgres -d halulu_api < halulu_api_backup.sql

# From compressed dump
pg_restore -U postgres -d halulu_api halulu_api_backup.dump
```

### Automated Backups

```bash
#!/bin/bash
# backup.sh
BACKUP_DIR="/backups"
DB_NAME="halulu_api"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

pg_dump -U postgres $DB_NAME | gzip > $BACKUP_DIR/${DB_NAME}_${TIMESTAMP}.sql.gz

# Keep only last 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete
```

Schedule with cron:
```
0 2 * * * /scripts/backup.sh  # Daily at 2 AM
```

## Rollback Procedures

### Rollback Migration

```bash
# See current migrations
dotnet ef migrations list

# Rollback to previous migration
dotnet ef database update <previous-migration-name>

# Or remove last migration entirely
dotnet ef migrations remove
```

### Rollback Code

```bash
# Revert to previous commit
git revert <commit-hash>

# Or reset hard (lose changes)
git reset --hard <commit-hash>
```

## Post-Deployment

### 1. Verify All Endpoints

Use Swagger UI to test all endpoints:
- Create service request ✓
- Update service request ✓
- Get service request ✓
- Get user requests ✓
- Cancel service request ✓
- Get all requests (admin) ✓

### 2. Load Testing

```bash
# Simple load test with Apache Bench
ab -n 1000 -c 10 http://localhost:5000/api/health/ping

# Or use k6
k6 run load-test.js
```

### 3. Security Audit

- [ ] Verify JWT validation working
- [ ] Test unauthorized access denied
- [ ] Verify cross-user access prevented
- [ ] Check sensitive data not logged
- [ ] Validate input sanitization

### 4. Documentation Update

- [ ] Update team documentation
- [ ] Share API documentation link
- [ ] Document environment variables
- [ ] Document backup procedures

## Monitoring Setup

### Application Insights (Azure)

```csharp
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:InstrumentationKey"]);
```

### Sentry (Error Tracking)

```csharp
SentrySdk.Init(o => o.Dsn = "your-sentry-dsn");
```

### Prometheus Metrics

```csharp
builder.Services.AddPrometheusActuatorServices();
```

## Maintenance Schedule

### Daily
- Monitor error logs
- Check disk space
- Verify API availability

### Weekly
- Review performance metrics
- Check database growth
- Backup verification

### Monthly
- Database maintenance (VACUUM ANALYZE)
- Security updates
- Performance optimization

## Support Contact

For deployment issues or questions, contact:
- **Email**: support@halulu.com
- **Slack**: #dev-support
- **Docs**: https://docs.halulu.com

---

**Last Updated**: 2024-11-01  
**Status**: Ready for Production  
**Next Steps**: Follow deployment steps above