# Getting Started with HaluluAPI

Your complete .NET Core 8 email OTP authentication system is ready!

## 🎯 What You Got

A production-ready Web API with:
- ✅ Email-based OTP authentication (6-digit, 5-minute expiry)
- ✅ JWT token management with role-based access
- ✅ PostgreSQL database with EF Core migrations
- ✅ SMTP email service with HTML templates
- ✅ Docker & Docker Compose support
- ✅ Swagger/OpenAPI documentation
- ✅ Global error handling & logging
- ✅ CORS support for mobile frontends
- ✅ Auto-user creation on first OTP verification
- ✅ Account lockout protection

## 📋 Prerequisites Check

Before you start, make sure you have:

```bash
# Check .NET version
dotnet --version
# Should be 8.0.x or higher

# Check Docker (optional)
docker --version
docker-compose --version

# Check PostgreSQL (if running locally)
psql --version
```

## 🚀 Quick Start (Choose One Option)

### Option A: Local Development (Recommended for beginners)

**Step 1: Setup Database**
```bash
# If PostgreSQL is not running, install it:
# Windows: choco install postgresql
# Mac: brew install postgresql
# Linux: sudo apt-get install postgresql

# Start PostgreSQL service
# Windows: Net start PostgreSQL15
# Mac/Linux: brew services start postgresql

# Create database and user (in psql)
psql -U postgres
postgres=# CREATE DATABASE halulu_db_dev;
postgres=# CREATE USER halulu WITH PASSWORD 'halulu123';
postgres=# ALTER ROLE halulu WITH CREATEDB;
postgres=# \q
```

**Step 2: Configure Application**
```bash
# Edit appsettings.Development.json
# Update connection string if needed
```

**Step 3: Install & Run**
```bash
# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run

# API will be available at:
# - API: https://localhost:5001
# - Swagger: https://localhost:5001/swagger
```

### Option B: Docker (Recommended for production)

**Step 1: Start Services**
```bash
# Start PostgreSQL in background
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
# Check status
docker-compose ps
```

**Step 2: Update Configuration**
```
appsettings.Development.json:
"DefaultConnection": "Host=postgres;Port=5432;Database=halulu_db_dev;Username=postgres;Password=postgres"
```

**Step 3: Run Application**
```bash
# Apply migrations
dotnet ef database update

# Run application
dotnet run
```

### Option C: Full Docker Stack

**Step 1: Build & Run**
```bash
# Uncomment the API service in docker-compose.yml first!

# Build and start
docker-compose up --build

# API will be available at:
# - http://localhost:5000
```

## ⚙️ Configuration Setup

### 1. Email Configuration (Gmail Example)

**Get Gmail App Password:**
1. Go to [Gmail Security](https://myaccount.google.com/security)
2. Enable 2-Step Verification
3. Generate App Password for "Mail" and "Windows Computer"
4. Copy the generated password

**Update appsettings.json:**
```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "your.email@gmail.com",
  "SenderPassword": "your_app_password_16_chars",
  "SenderName": "Halulu Authentication"
}
```

### 2. JWT Secret Configuration

**Generate Secure Key:**
```powershell
# Windows PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object {[byte](Get-Random -Maximum 256)}))
```

**Update appsettings.json:**
```json
"JwtSettings": {
  "SecretKey": "your_generated_base64_secret_key_here",
  "Issuer": "HaluluAPI",
  "Audience": "HaluluMobileApp",
  "ExpirationMinutes": 1440
}
```

### 3. CORS Configuration (for mobile app)

**Update appsettings.json:**
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",      // React/Vue dev
    "http://localhost:8081",       // Flutter dev
    "http://localhost:5173",       // Vite dev
    "https://yourdomain.com"       // Production
  ]
}
```

## 🧪 Test the API

### Quick Test Using Swagger UI

1. Open `https://localhost:5001/swagger`
2. Click on any endpoint to expand
3. Click "Try it out"
4. Enter test data
5. Click "Execute"

### Manual Testing with cURL

**1. Send OTP**
```bash
curl -X POST https://localhost:5001/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "phoneNumber": "+1234567890"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "OTP sent successfully to your email",
  "email": "test@example.com",
  "expirationMinutes": 5
}
```

> ⚠️ For testing without real email, check database for OTP code:
```sql
SELECT "OtpCode", "ExpiresAt" FROM "OtpRecords" 
WHERE "Email" = 'test@example.com' 
ORDER BY "CreatedAt" DESC LIMIT 1;
```

**2. Verify OTP**
```bash
curl -X POST https://localhost:5001/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "otpCode": "123456"
  }'
```

**Expected Response (note the token):**
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "test@example.com",
    "firstName": null,
    "lastName": null,
    "role": 0,
    "isProfileComplete": false
  },
  "isNewUser": true
}
```

**3. Register/Complete Profile**
```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1234567890",
    "role": 0
  }'
```

**4. Get Current User (Protected)**
```bash
curl -X GET https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

## 📊 Database Setup

### Create Database Manually (if needed)

```sql
-- Connect to PostgreSQL as admin
psql -U postgres

-- Create database
CREATE DATABASE halulu_db_dev;

-- Create user
CREATE USER halulu WITH PASSWORD 'halulu123';

-- Grant permissions
ALTER ROLE halulu WITH CREATEDB;
GRANT ALL PRIVILEGES ON DATABASE halulu_db_dev TO halulu;
```

### Run Migrations

```bash
# Create initial migration
dotnet ef migrations add InitialMigration

# Apply to database
dotnet ef database update

# View migration status
dotnet ef migrations list

# Rollback if needed
dotnet ef database update LastWorkingMigration
```

### Check Database Structure

```bash
# Connect to database
psql -U halulu -d halulu_db_dev

# List tables
\dt

# Check Users table
\d "Users"

# Check OtpRecords table
\d "OtpRecords"

# View sample data
SELECT * FROM "Users" LIMIT 5;
SELECT * FROM "OtpRecords" LIMIT 5;
```

## 📁 Project Files Overview

| File | Purpose |
|------|---------|
| `Program.cs` | Application startup & configuration |
| `appsettings.json` | Configuration (keep secrets safe!) |
| `Models/` | Database entities |
| `DTOs/` | Request/response models |
| `Services/` | Business logic |
| `Controllers/` | API endpoints |
| `Data/` | Entity Framework setup |
| `Migrations/` | Database changes history |

## 🔐 Security Checklist

- [ ] Change JWT secret key to something strong
- [ ] Use Gmail App Password (not regular password)
- [ ] Configure CORS with your actual domain
- [ ] Store secrets in environment variables (production)
- [ ] Enable HTTPS in production
- [ ] Use strong database password
- [ ] Enable database backups
- [ ] Review SQL logs for suspicious activity

## 🐛 Common Issues & Solutions

### Issue: "Connection refused" PostgreSQL error
```
Solution: Make sure PostgreSQL is running
- Windows: Services → PostgreSQL → Start
- Mac: brew services start postgresql
- Docker: docker-compose up -d postgres
```

### Issue: Email not sending
```
Solution: Check your configuration
1. Verify SMTP credentials are correct
2. For Gmail: Use App Password, not regular password
3. Check firewall allows port 587
4. Enable "Less secure apps" if needed
5. Check application logs for errors
```

### Issue: JWT token invalid
```
Solution: Ensure correct configuration
1. Verify JWT secret key hasn't changed
2. Check token hasn't expired (default 24 hours)
3. Verify token format: "Bearer <token>"
4. Check Authorization header is present
```

### Issue: Migrations fail
```
Solution: Reset migrations
1. Delete latest migration file
2. Run: dotnet ef migrations remove
3. Run: dotnet ef migrations add InitialMigration
4. Run: dotnet ef database update
```

## 📖 Documentation

For more detailed information, see:
- **[QUICK_START.md](QUICK_START.md)** - 5-minute setup
- **[README.md](README.md)** - Full documentation
- **[API_ENDPOINTS.md](API_ENDPOINTS.md)** - API reference
- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Production deployment
- **[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)** - Code organization

## 🚀 Next Steps

### Immediate (Today)
1. ✅ Get API running locally
2. ✅ Test OTP flow in Swagger UI
3. ✅ Verify email sending works

### Short-term (This Week)
1. ✅ Integrate with your mobile app
2. ✅ Test end-to-end authentication
3. ✅ Customize email templates
4. ✅ Set up logging & monitoring

### Long-term (This Month)
1. ✅ Deploy to Azure/Render
2. ✅ Configure production database
3. ✅ Set up backup strategy
4. ✅ Add SMS OTP (Twilio integration)
5. ✅ Implement analytics

## 💡 Tips & Tricks

**Development**
```bash
# Hot reload with dotnet watch
dotnet watch run

# Debug mode
dotnet run --configuration Debug

# View logs
tail -f logs/application.log

# Reset database completely
dotnet ef database drop --force
dotnet ef database update
```

**Database**
```bash
# Backup database
pg_dump -U halulu halulu_db_dev > backup.sql

# Restore database
psql -U halulu halulu_db_dev < backup.sql
```

**Docker**
```bash
# View logs
docker-compose logs -f api

# Stop all services
docker-compose down

# Remove volumes (careful!)
docker-compose down -v
```

## 📞 Support

- 📚 Documentation: Check the README & MD files
- 🐛 Issues: Review common troubleshooting section
- 💻 Code: Review inline comments in services
- 🔍 Logs: Check Application Insights (after deployment)

## 🎓 Learning Path

1. **Understand the flow**: Read API_ENDPOINTS.md
2. **Review architecture**: Check PROJECT_STRUCTURE.md
3. **Extend features**: Modify services in /Services
4. **Add endpoints**: Create new methods in controllers
5. **Deploy**: Follow DEPLOYMENT.md

## 🎉 You're Ready!

Your API is configured and ready to use. Start building amazing features!

Questions? Check the documentation files or review the code comments.

---

**Version:** 1.0.0
**Last Updated:** January 2024
**Status:** Production Ready ✅