# Halulu API - Email OTP Authentication

A production-ready .NET Core 8 Web API with email-based OTP authentication, JWT token management, and PostgreSQL integration.

## Features

✅ **Email-based OTP Authentication**
- Generate and send 6-digit OTP to email
- OTP expiration (configurable, default 5 minutes)
- Maximum attempt validation with email locking
- Auto-create user on first OTP verification

✅ **JWT Token Management**
- JWT-based session management
- Configurable token expiration
- Role-based access control (Requester, Provider, Admin)
- Token validation and user extraction

✅ **User Management**
- User registration with profile completion
- Role detection and assignment
- Profile status tracking
- Last login tracking

✅ **Email Service**
- SMTP configuration (Gmail, Office 365, custom)
- HTML email templates
- OTP, welcome, and password reset emails
- Error handling and retry logic

✅ **Security Features**
- CORS support for mobile frontends
- Email validation and phone number verification
- Account lockout after failed OTP attempts
- Secure password practices

✅ **API Documentation**
- Swagger/OpenAPI integration
- JWT Bearer authentication in Swagger UI
- Comprehensive endpoint documentation
- Health check endpoints

✅ **Future-Ready Architecture**
- Abstracted services for easy SMS OTP integration
- Extensible authentication flow
- Structured for mobile and web clients

## Project Structure

```
HaluluAPI/
├── Models/                 # Data models (User, OtpRecord)
├── DTOs/                   # Data transfer objects
├── Data/                   # Entity Framework DbContext
├── Services/               # Business logic services
│   ├── IOtpService        # OTP generation and validation
│   ├── IEmailService      # Email sending
│   ├── IJwtService        # JWT token handling
│   └── IAuthService       # Authentication business logic
├── Controllers/            # API endpoints
├── Program.cs             # Application configuration
├── appsettings.json       # Configuration
└── appsettings.Development.json
```

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL 12 or higher
- Visual Studio 2022 or VS Code

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/HaluluAPI.git
cd HaluluAPI
```

### 2. Install Dependencies

```bash
dotnet restore
```

### 3. Configure Database

Update `appsettings.json` with your PostgreSQL connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=halulu_db;Username=postgres;Password=your_password"
}
```

### 4. Configure Email Settings

Update `appsettings.json` with your email provider:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "your_email@gmail.com",
  "SenderPassword": "your_app_password",
  "SenderName": "Halulu"
}
```

**For Gmail:**
- Use an [App Password](https://support.google.com/accounts/answer/185833) instead of your regular password
- Enable [Less secure app access](https://myaccount.google.com/lesssecureapps) if needed

### 5. Configure JWT Settings

Update `appsettings.json` with secure JWT configuration:

```json
"JwtSettings": {
  "SecretKey": "your_super_secret_key_min_32_characters_long_!@#$%",
  "Issuer": "HaluluAPI",
  "Audience": "HaluluMobileApp",
  "ExpirationMinutes": 1440
}
```

### 6. Create and Migrate Database

```bash
dotnet ef migrations add InitialMigration
dotnet ef database update
```

### 7. Run the Application

```bash
dotnet run
```

The API will be available at `https://localhost:5001` and Swagger UI at `https://localhost:5001/swagger`

## API Endpoints

### Authentication

#### 1. Send OTP
```http
POST /api/auth/send-otp
Content-Type: application/json

{
  "email": "user@example.com",
  "phoneNumber": "+1234567890"
}
```

**Response:**
```json
{
  "success": true,
  "message": "OTP sent successfully to your email",
  "email": "user@example.com",
  "expirationMinutes": 5
}
```

#### 2. Verify OTP
```http
POST /api/auth/verify-otp
Content-Type: application/json

{
  "email": "user@example.com",
  "otpCode": "123456"
}
```

**Response:**
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "phoneNumber": "unknown",
    "firstName": null,
    "lastName": null,
    "role": "Requester",
    "isProfileComplete": false,
    "isActive": true,
    "createdAt": "2024-01-15T10:30:00Z"
  },
  "isNewUser": true
}
```

#### 3. Register/Complete Profile
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "role": "Requester"
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "phoneNumber": "+1234567890",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Requester",
  "isProfileComplete": true,
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastLoginAt": "2024-01-15T10:35:00Z"
}
```

#### 4. Get Current User (Protected)
```http
GET /api/auth/me
Authorization: Bearer <token>
```

#### 5. Health Check
```http
GET /api/health/ping
GET /api/health/status
```

## Configuration

### OTP Settings

```json
"OtpSettings": {
  "ExpirationMinutes": 5,
  "OtpLength": 6
}
```

### CORS Configuration

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:8081",
    "http://localhost:5173"
  ]
}
```

## Database Schema

### Users Table
- `Id` (GUID, Primary Key)
- `Email` (VARCHAR, Unique)
- `PhoneNumber` (VARCHAR, Unique)
- `FirstName` (VARCHAR)
- `LastName` (VARCHAR)
- `ProfileImageUrl` (VARCHAR)
- `Role` (INT) - 0: Requester, 1: Provider, 2: Admin
- `IsActive` (BOOL)
- `IsProfileComplete` (BOOL)
- `CreatedAt` (TIMESTAMP)
- `UpdatedAt` (TIMESTAMP)
- `LastLoginAt` (TIMESTAMP)

### OtpRecords Table
- `Id` (GUID, Primary Key)
- `Email` (VARCHAR)
- `OtpCode` (VARCHAR)
- `ExpiresAt` (TIMESTAMP)
- `IsUsed` (BOOL)
- `UsedAt` (TIMESTAMP)
- `CreatedAt` (TIMESTAMP)
- `AttemptCount` (INT)
- `MaxAttempts` (INT)
- `IsLocked` (BOOL)
- `UserId` (GUID, Foreign Key)
- `Purpose` (INT) - 0: Authentication, 1: PhoneVerification, 2: PasswordReset

## Extending for Mobile OTP (SMS)

To add SMS OTP support later:

1. Create `ISmsService` interface
2. Implement SMS provider (Twilio, AWS SNS, etc.)
3. Add SMS sending in `SendOtpAsync` method based on purpose
4. Update `OtpPurpose` enum with phone verification specifics

## Deployment

### Azure App Service
1. Create App Service and PostgreSQL database
2. Update connection string in Azure Key Vault
3. Deploy using Visual Studio or GitHub Actions
4. Configure environment variables for JWT and email settings

### Render or Heroku
1. Create PostgreSQL database
2. Set environment variables
3. Deploy from GitHub
4. Configure custom domain

## Security Considerations

⚠️ **Production Checklist:**
- [ ] Use strong JWT secret key (32+ characters, random)
- [ ] Store secrets in Key Vault or secure configuration service
- [ ] Enable HTTPS only
- [ ] Use app-specific passwords for email service
- [ ] Implement rate limiting on OTP endpoints
- [ ] Add request logging and monitoring
- [ ] Configure proper CORS origins
- [ ] Use environment-specific configurations
- [ ] Enable database encryption
- [ ] Implement audit logging

## Troubleshooting

### Email Not Sending
- Check SMTP credentials
- Verify firewall allows outbound SMTP (port 587)
- Check email logs in application insights
- Verify sender email is authorized

### Database Connection Issues
- Confirm PostgreSQL is running
- Verify connection string format
- Check database user permissions
- Verify network connectivity

### JWT Token Issues
- Ensure secret key matches between generation and validation
- Check token expiration time
- Verify issuer and audience match

## Contributing

1. Create a feature branch (`git checkout -b feature/amazing-feature`)
2. Commit changes (`git commit -m 'Add amazing feature'`)
3. Push to branch (`git push origin feature/amazing-feature`)
4. Open a Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues and questions, please create an issue in the GitHub repository.

## Roadmap

- [ ] SMS OTP integration (Twilio)
- [ ] Biometric authentication
- [ ] Two-factor authentication
- [ ] Social login integration (Google, Facebook)
- [ ] User profile management API
- [ ] Admin dashboard
- [ ] Analytics and reporting
- [ ] Multi-language support