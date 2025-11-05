# Deployment Guide

## Local Development

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL 12+
- Visual Studio 2022 / VS Code

### Setup

1. **Clone and restore:**
```bash
git clone <repo-url>
cd HaluluAPI
dotnet restore
```

2. **Configure appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=halulu_db_dev;Username=postgres;Password=postgres"
  }
}
```

3. **Create database:**
```bash
dotnet ef database update
```

4. **Run application:**
```bash
dotnet run
```

Access Swagger UI: `https://localhost:5001/swagger`

---

## Docker Development

### Run PostgreSQL in Docker

```bash
docker-compose up -d postgres
```

Update `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=halulu_db_dev;Username=postgres;Password=postgres"
  }
}
```

### Run Full Stack

```bash
docker-compose up
```

---

## Deployment to Azure App Service

### Step 1: Create Resources

```bash
# Create Resource Group
az group create --name halulu-rg --location eastus

# Create App Service Plan
az appservice plan create \
  --name halulu-plan \
  --resource-group halulu-rg \
  --sku B1 \
  --is-linux

# Create App Service
az webapp create \
  --resource-group halulu-rg \
  --plan halulu-plan \
  --name halulu-api \
  --runtime "DOTNETCORE|8.0"

# Create PostgreSQL Server
az postgres flexible-server create \
  --resource-group halulu-rg \
  --name halulu-postgres \
  --admin-user azureuser \
  --admin-password <password> \
  --sku-name Standard_B1ms \
  --tier Burstable
```

### Step 2: Configure Application Settings

```bash
az webapp config appsettings set \
  --resource-group halulu-rg \
  --name halulu-api \
  --settings \
  ConnectionStrings__DefaultConnection="Host=halulu-postgres.postgres.database.azure.com;Port=5432;Database=halulu_db;Username=azureuser;Password=<password>;" \
  JwtSettings__SecretKey="<your_secret_key>" \
  EmailSettings__SmtpHost="smtp.gmail.com" \
  EmailSettings__SmtpPort="587" \
  EmailSettings__SenderEmail="<your_email>" \
  EmailSettings__SenderPassword="<your_app_password>" \
  ASPNETCORE_ENVIRONMENT="Production"
```

### Step 3: Deploy Application

#### Option A: Using GitHub Actions

Create `.github/workflows/deploy.yml`:
```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Publish
        run: dotnet publish --configuration Release --output ./publish

      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: halulu-api
          package: ./publish
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
```

#### Option B: Using Azure CLI

```bash
az webapp deployment source config-zip \
  --resource-group halulu-rg \
  --name halulu-api \
  --src ./publish.zip
```

#### Option C: Using Visual Studio

1. Right-click project → Publish
2. Select "Azure" → "Azure App Service"
3. Configure and deploy

### Step 4: Run Database Migrations

```bash
az webapp remote-build enabled --ids /subscriptions/<sub-id>/resourceGroups/halulu-rg/providers/Microsoft.Web/sites/halulu-api

# After deployment, run migrations:
dotnet ef database update
```

---

## Deployment to Render

### Step 1: Create PostgreSQL Database

1. Go to [Render Dashboard](https://dashboard.render.com)
2. Create new PostgreSQL Database
3. Note connection string

### Step 2: Create Web Service

1. Create new Web Service
2. Connect GitHub repository
3. Configure:
   - **Runtime:** .NET 8
   - **Build Command:** `dotnet build --configuration Release`
   - **Start Command:** `dotnet HaluluAPI.dll`

### Step 3: Set Environment Variables

In Render dashboard, add:
```
ConnectionStrings__DefaultConnection=<postgres-connection-string>
JwtSettings__SecretKey=<your_secret_key>
EmailSettings__SmtpHost=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__SenderEmail=<your_email>
EmailSettings__SenderPassword=<your_app_password>
ASPNETCORE_ENVIRONMENT=Production
```

### Step 4: Deploy

Push to main branch - deployment will start automatically.

---

## Production Checklist

### Security
- [ ] JWT secret key is strong (32+ characters, random)
- [ ] Store secrets in Key Vault/Secrets Manager
- [ ] Enable HTTPS only
- [ ] Configure CORS with specific origins (not *)
- [ ] Use environment-specific configuration
- [ ] Database credentials in secrets, not in code
- [ ] Enable database encryption
- [ ] Implement rate limiting
- [ ] Add request validation
- [ ] Enable Web Application Firewall

### Performance
- [ ] Enable database connection pooling
- [ ] Configure caching headers
- [ ] Enable compression
- [ ] Use CDN for static assets
- [ ] Enable auto-scaling
- [ ] Set up monitoring and alerting
- [ ] Configure log aggregation

### Monitoring & Logging
- [ ] Set up Application Insights
- [ ] Configure log retention policies
- [ ] Set up performance metrics
- [ ] Configure alerting for errors
- [ ] Track API usage and performance
- [ ] Monitor database performance
- [ ] Set up user behavior tracking

### Database
- [ ] Regular backups enabled
- [ ] Point-in-time restore configured
- [ ] Database encryption enabled
- [ ] Firewall rules configured
- [ ] SSL/TLS enforced
- [ ] Regular maintenance scheduled
- [ ] Performance optimization done

### API & Documentation
- [ ] Swagger/OpenAPI updated
- [ ] API versioning strategy implemented
- [ ] Rate limiting configured
- [ ] Request/response logging enabled
- [ ] Error handling tested
- [ ] Documentation up-to-date

---

## Scaling Strategy

### Vertical Scaling
Increase App Service/VM size for better performance

### Horizontal Scaling
- Enable auto-scaling based on CPU/memory
- Load balancing with multiple instances
- Database connection pooling

### Database Optimization
- Create indexes on frequently queried columns
- Archive old OTP records
- Enable query caching

### Caching Layer
Consider Redis for:
- Session caching
- Rate limiting
- OTP lookup optimization

---

## Monitoring & Alerts

### Application Insights Setup
```bash
az monitor app-insights component create \
  --app halulu-ai \
  --location eastus \
  --resource-group halulu-rg
```

### Key Metrics to Monitor
- Request rate and latency
- Error rate and types
- OTP generation/verification success rate
- Email sending success rate
- User authentication patterns
- Database query performance
- Server resource usage

### Set Up Alerts
- High error rate (> 5%)
- Email sending failures
- Database connection issues
- Slow queries (> 1s)
- Unauthorized access attempts

---

## Troubleshooting

### Connection String Issues
```bash
# Test connection
az postgres flexible-server execute \
  --name halulu-postgres \
  --admin-user azureuser \
  -d postgres \
  -c "SELECT 1;"
```

### Application Not Starting
```bash
# View logs
az webapp log tail --resource-group halulu-rg --name halulu-api
```

### Database Migration Failures
```bash
# Connect and check migration status
dotnet ef migrations list
dotnet ef database update --verbose
```

---

## Rollback Procedure

### Azure App Service
```bash
# View deployment history
az webapp deployment list --resource-group halulu-rg --name halulu-api

# Rollback to specific version
az webapp deployment source update-metadata \
  --name halulu-api \
  --resource-group halulu-rg \
  --deployment-id <deployment-id>
```

### Database Rollback
```bash
# List migrations
dotnet ef migrations list

# Rollback to specific migration
dotnet ef database update <previous-migration-name>
```

---

## Support & Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Render Deployment Docs](https://render.com/docs)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [ASP.NET Core Deployment](https://docs.microsoft.com/aspnet/core/host-and-deploy/)