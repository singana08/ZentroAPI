# Project Structure Overview

Complete file and folder structure for HaluluAPI.

## Directory Tree

```
HaluluAPI/
│
├── 📂 Models/                          # Domain models
│   ├── User.cs                         # User entity with roles and profile
│   └── OtpRecord.cs                    # OTP records with expiry and attempts
│
├── 📂 DTOs/                            # Data Transfer Objects
│   ├── AuthRequests.cs                 # Request models (SendOtp, VerifyOtp, Register)
│   └── AuthResponses.cs                # Response models (Token, User, Error)
│
├── 📂 Data/                            # Entity Framework
│   └── ApplicationDbContext.cs          # DbContext configuration
│
├── 📂 Services/                        # Business Logic Services
│   ├── Interfaces/
│   │   ├── IOtpService.cs             # OTP generation & validation
│   │   ├── IEmailService.cs           # Email sending
│   │   ├── IJwtService.cs             # JWT token management
│   │   └── IAuthService.cs            # Authentication business logic
│   │
│   ├── OtpService.cs                   # OTP implementation (6-digit, 5-min expiry)
│   ├── EmailService.cs                 # SMTP email with HTML templates
│   ├── JwtService.cs                   # JWT token generation & validation
│   └── AuthService.cs                  # Auth workflows (auto-user creation)
│
├── 📂 Controllers/                     # API Endpoints
│   ├── AuthController.cs               # /api/auth endpoints
│   └── HealthController.cs             # /api/health endpoints
│
├── 📂 Utilities/                       # Helper Classes
│   ├── ClaimsPrincipalExtensions.cs    # JWT claims extraction
│   └── ValidationHelper.cs             # Input validation
│
├── 📂 Middleware/                      # HTTP Middleware
│   ├── ErrorHandlingMiddleware.cs      # Global error handling
│   └── RequestLoggingMiddleware.cs     # HTTP request/response logging
│
├── 📂 Migrations/                      # Entity Framework Migrations
│   ├── InitialMigration.cs
│   └── ...future migrations
│
├── 📄 Program.cs                       # Application configuration & startup
├── 📄 HaluluAPI.csproj                 # Project file with dependencies
│
├── 📄 appsettings.json                 # Configuration (with secrets template)
├── 📄 appsettings.Development.json     # Development-specific config
│
├── 📄 docker-compose.yml               # Docker services (PostgreSQL + API)
├── 📄 Dockerfile                       # Multi-stage Docker build
├── 📄 .dockerignore                    # Docker build ignore
│
├── 📄 .gitignore                       # Git ignore rules
├── 📄 .env.example                     # Environment variables template
│
├── 📄 README.md                        # Full documentation
├── 📄 QUICK_START.md                   # 5-minute setup guide
├── 📄 DEPLOYMENT.md                    # Azure/Render deployment guide
├── 📄 API_ENDPOINTS.md                 # API reference documentation
├── 📄 PROJECT_STRUCTURE.md             # This file
│
└── 📄 GlobalUsings.cs                  # Global using statements
```

## File Descriptions

### Models (Domain Layer)

**User.cs**
- Represents a user in the system
- Properties: Email, PhoneNumber, FirstName, LastName, Role, IsProfileComplete
- Roles: Requester (0), Provider (1), Admin (2)
- Navigation: OtpRecords collection

**OtpRecord.cs**
- Represents an OTP record
- Properties: Email, OtpCode, ExpiresAt, IsUsed, AttemptCount
- Purposes: Authentication (0), PhoneVerification (1), PasswordReset (2)
- Foreign key to User (nullable for pre-registration OTPs)

### DTOs (Transfer Layer)

**AuthRequests.cs**
- `SendOtpRequest`: Email, PhoneNumber, Purpose
- `VerifyOtpRequest`: Email, OtpCode, Purpose
- `RegisterRequest`: Email, FirstName, LastName, PhoneNumber, Role
- Includes validation attributes and enums

**AuthResponses.cs**
- `SendOtpResponse`: Success, Message, Email, ExpirationMinutes
- `VerifyOtpResponse`: Success, Message, Token, User, IsNewUser
- `UserDto`: User information for responses
- `ErrorResponse`: Error details with validation errors

### Services (Business Logic)

**IOtpService**
- `GenerateOtpAsync()`: Create 6-digit OTP with 5-min expiry
- `VerifyOtpAsync()`: Validate OTP, track attempts, lock on max attempts
- `InvalidateOtpAsync()`: Mark OTP as used
- `IsEmailLockedAsync()`: Check if email is temporarily locked
- `GetOtpRemainingTimeAsync()`: Get remaining time for current OTP

**IEmailService**
- `SendOtpEmailAsync()`: Send OTP with HTML template
- `SendWelcomeEmailAsync()`: Welcome email after registration
- `SendPasswordResetEmailAsync()`: Password reset email

**IJwtService**
- `GenerateToken()`: Create JWT with user claims
- `ValidateToken()`: Validate JWT signature and expiry
- `GetUserIdFromToken()`: Extract user ID from token

**IAuthService**
- `SendOtpAsync()`: Send OTP to email
- `VerifyOtpAsync()`: Verify OTP, create user if new
- `RegisterAsync()`: Complete user profile
- `GetCurrentUserAsync()`: Get authenticated user
- `GetUserByEmailAsync()`: Lookup user by email

### Controllers (API Layer)

**AuthController**
- `POST /api/auth/send-otp`: Send OTP to email
- `POST /api/auth/verify-otp`: Verify OTP and get JWT
- `POST /api/auth/register`: Complete user profile
- `GET /api/auth/me`: Get current user (protected)
- `GET /api/auth/user/{email}`: Get user by email (protected)

**HealthController**
- `GET /api/health/ping`: Quick health check
- `GET /api/health/status`: Detailed status

### Utilities

**ClaimsPrincipalExtensions**
- `GetUserId()`: Extract user ID from JWT claims
- `GetEmail()`: Extract email from claims
- `GetRole()`: Extract role from claims
- `IsProfileComplete()`: Check profile completion status

**ValidationHelper**
- `IsValidEmail()`: Email format validation
- `IsValidPhoneNumber()`: Phone format validation
- `IsValidOtp()`: OTP format validation
- `IsValidName()`: Name format validation

### Middleware

**ErrorHandlingMiddleware**
- Catches all exceptions
- Returns consistent error responses
- Logs errors for debugging

**RequestLoggingMiddleware**
- Logs all HTTP requests
- Logs response status and duration
- Excludes authorization headers from logs

### Database (Data Layer)

**ApplicationDbContext**
- `DbSet<User>`: Users table
- `DbSet<OtpRecord>`: OTP records table
- Unique indexes on Email and PhoneNumber
- Foreign key constraints

### Configuration Files

**Program.cs**
- DbContext configuration
- Service registration (Dependency Injection)
- Authentication (JWT)
- Authorization
- CORS setup
- Swagger configuration
- Database migration on startup

**appsettings.json**
- Database connection string
- JWT settings (secret, issuer, audience, expiration)
- OTP settings (expiration, length)
- Email settings (SMTP host, port, credentials)
- CORS allowed origins

**appsettings.Development.json**
- Overrides for development environment
- Debug logging levels
- Local database credentials

### Docker Configuration

**Dockerfile**
- Multi-stage build (SDK → Runtime)
- Restores, builds, and publishes
- Runs as production image

**docker-compose.yml**
- PostgreSQL 15 Alpine service
- Volume for data persistence
- Health checks
- Optional API service (commented)

### Documentation

**README.md**
- Project overview and features
- Installation instructions
- Configuration guide
- API endpoint examples
- Database schema
- Deployment instructions
- Troubleshooting guide

**QUICK_START.md**
- 5-minute setup guide
- Docker quick start
- API testing examples
- Configuration quick reference

**DEPLOYMENT.md**
- Local development setup
- Docker development
- Azure App Service deployment
- Render/Heroku deployment
- Production checklist
- Scaling strategies
- Monitoring setup

**API_ENDPOINTS.md**
- Complete API reference
- Request/response examples
- Error codes and handling
- Authentication details
- Rate limiting info
- Example workflows

## Key Features by File

### Authentication Flow
1. User sends email → `AuthController.SendOtp()`
2. `AuthService.SendOtpAsync()` → `OtpService.GenerateOtpAsync()`
3. `EmailService.SendOtpEmailAsync()` → SMTP email sent
4. User verifies OTP → `AuthController.VerifyOtp()`
5. `AuthService.VerifyOtpAsync()` → `OtpService.VerifyOtpAsync()`
6. Auto-create user if new
7. `JwtService.GenerateToken()` → Return JWT
8. User registers profile → `AuthController.Register()`
9. `AuthService.RegisterAsync()` → Update user profile
10. `EmailService.SendWelcomeEmailAsync()` → Welcome email

### Security Features
- OTP expiry (5 minutes configurable)
- Max attempt validation (5 attempts)
- Email lockout (1 hour after max attempts)
- JWT token expiry (24 hours configurable)
- Global error handling (no stack traces in production)
- Request logging middleware
- CORS validation

### Future Extensibility
- `IOtpService` abstraction for SMS OTP (Twilio, AWS SNS)
- `IEmailService` abstraction for other email providers
- `IJwtService` for token refresh mechanisms
- Middleware layer for rate limiting, authentication
- Repository pattern ready in DbContext
- Service layer decoupled from controllers

## Dependencies

### NuGet Packages
- **Microsoft.EntityFrameworkCore** (8.0.0)
- **Npgsql.EntityFrameworkCore.PostgreSQL** (8.0.0)
- **Microsoft.AspNetCore.Authentication.JwtBearer** (8.0.0)
- **System.IdentityModel.Tokens.Jwt** (8.0.0)
- **Swashbuckle.AspNetCore** (6.5.0)
- **MailKit** (4.5.0)

### Frameworks
- .NET Core 8.0
- PostgreSQL 12+
- SMTP Email Service

## Development Workflow

1. **Add new endpoint**
   - Create DTO in `/DTOs`
   - Add method to service interface in `/Services`
   - Implement in service class
   - Add controller endpoint in `/Controllers`
   - Update Swagger docs with `/// <summary>`

2. **Add new model**
   - Create in `/Models`
   - Add DbSet in `ApplicationDbContext`
   - Configure in `OnModelCreating()`
   - Create migration: `dotnet ef migrations add ModelName`

3. **Add new service**
   - Create interface in `/Services`
   - Implement service class
   - Register in `Program.cs`: `services.AddScoped<IService, Service>()`

4. **Add middleware**
   - Create in `/Middleware`
   - Register in `Program.cs`: `app.UseMiddleware<CustomMiddleware>()`

5. **Deploy**
   - Update appsettings.json for production
   - Run migrations on target database
   - Deploy Docker image or publish to App Service
   - Follow DEPLOYMENT.md guide

## Best Practices Used

✅ **Dependency Injection** - All dependencies injected via constructor
✅ **Interface Segregation** - Services behind interfaces
✅ **Async/Await** - All I/O operations async
✅ **Error Handling** - Global middleware + try-catch in services
✅ **Logging** - Structured logging with ILogger
✅ **Validation** - Data annotations + custom validators
✅ **Security** - JWT, CORS, input validation, secrets in config
✅ **Documentation** - XML comments on public members
✅ **Separation of Concerns** - Clear layer separation
✅ **Configuration** - External config via appsettings.json

---

**Last Updated:** 2024-01-15
**Total Files:** 30+
**Total Lines of Code:** 2000+