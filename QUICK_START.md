# Quick Start Guide

Get HaluluAPI up and running in 5 minutes!

## Option 1: Local Development (Fastest)

### Prerequisites
- .NET 8.0 SDK installed
- PostgreSQL running on localhost:5432

### Steps

1. **Clone the repository**
   ```bash
   cd HaluluAPI
   ```

2. **Update database connection** (appsettings.Development.json)
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=halulu_db_dev;Username=postgres;Password=postgres"
   }
   ```

3. **Create database**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Open Swagger UI**
   - Navigate to `https://localhost:5001/swagger`
   - API is ready! 🎉

---

## Option 2: Docker (Recommended)

### Prerequisites
- Docker and Docker Compose installed

### Steps

1. **Start PostgreSQL**
   ```bash
   docker-compose up -d postgres
   ```

2. **Update connection string** (appsettings.Development.json)
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=postgres;Port=5432;Database=halulu_db_dev;Username=postgres;Password=postgres"
   }
   ```

3. **Create database and run**
   ```bash
   dotnet ef database update
   dotnet run
   ```

---

## Test the API

### 1. Send OTP

```bash
curl -X POST http://localhost:5000/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "phoneNumber": "+1234567890"
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "OTP sent successfully to your email",
  "email": "test@example.com",
  "expirationMinutes": 5
}
```

### 2. Verify OTP

```bash
curl -X POST http://localhost:5000/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "otpCode": "123456"
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {...},
  "isNewUser": true
}
```

### 3. Register/Complete Profile

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1234567890",
    "role": 0
  }'
```

### 4. Get Current User (Protected)

```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Configuration Quick Reference

### Email (Gmail Example)

1. **Enable 2-Factor Authentication** on Google Account
2. **Create App Password**: [Google App Passwords](https://myaccount.google.com/apppasswords)
3. **Update appsettings.json**:
   ```json
   "EmailSettings": {
     "SmtpHost": "smtp.gmail.com",
     "SmtpPort": 587,
     "SenderEmail": "your.email@gmail.com",
     "SenderPassword": "your_app_password",
     "SenderName": "Halulu"
   }
   ```

### JWT Secret

Generate a strong secret key:
```bash
# On Linux/Mac
openssl rand -base64 32

# On Windows PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object {[byte](Get-Random -Maximum 256)}) )
```

Update in appsettings.json:
```json
"JwtSettings": {
  "SecretKey": "your_generated_secret_key_here"
}
```

---

## Useful Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests (when available)
dotnet test

# Create database migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# View migration history
dotnet ef migrations list

# Generate database drop script
dotnet ef database script

# Clean build
dotnet clean
dotnet build
```

---

## Troubleshooting

### "Connection refused" error
- Ensure PostgreSQL is running
- Verify connection string matches your database setup
- Check database credentials

### "Email sending failed"
- Verify SMTP credentials are correct
- Check firewall allows outbound SMTP (port 587)
- For Gmail, use App Password (not regular password)

### "JWT validation failed"
- Ensure JWT secret key matches in configuration
- Check token hasn't expired
- Verify Authorization header format: `Bearer <token>`

### "Database already exists"
- Delete the database: `DROP DATABASE halulu_db_dev;`
- Recreate: `dotnet ef database update`

---

## Next Steps

1. ✅ Run locally and test
2. 📖 Read [README.md](README.md) for full documentation
3. 🚀 Check [DEPLOYMENT.md](DEPLOYMENT.md) for production setup
4. 📱 Integrate with your mobile/web frontend
5. 🔧 Customize email templates and OTP settings

---

## Support

- 📚 Full documentation: [README.md](README.md)
- 🚀 Deployment guide: [DEPLOYMENT.md](DEPLOYMENT.md)
- 🐛 Report issues on GitHub
- 💬 Check GitHub Discussions

---

## Environment Files

Copy `.env.example` to `.env` and update values:
```bash
cp .env.example .env
```

---

Happy coding! 🎉