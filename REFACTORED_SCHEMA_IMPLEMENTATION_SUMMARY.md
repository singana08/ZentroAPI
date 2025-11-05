# Refactored Dual-Role Schema - Implementation Summary

## ✅ What's Been Created

### 1. Models
- ✅ **User.cs** - Simplified to core identity only
  - Columns: id, full_name, email, phone_number, profile_image, created_at, updated_at, last_login_at
  - Navigation: RequesterProfile (1-to-1), ProviderProfile (1-to-1)

- ✅ **Requester.cs** - New role-specific model
  - Columns: id, user_id, address, preferred_categories, is_active, created_at, updated_at
  - 1-to-1 relationship with User

- ✅ **Provider.cs** - New role-specific model
  - Columns: id, user_id, service_categories, experience_years, bio, service_areas, pricing_model, documents (JSON), availability_slots (JSONB), rating, earnings, is_active, created_at, updated_at
  - 1-to-1 relationship with User

### 2. Database Context
- ✅ **ApplicationDbContext.cs** - Updated
  - Added DbSet for Requester and Provider
  - Configured relationships for both new tables
  - Added proper indexes for performance

### 3. Database Migration
- ✅ **20251115120000_RefactorDualRoleSchema.cs** - Complete migration file
  - Creates requesters table
  - Creates providers table
  - Removes old columns from users table
  - Adds all indexes
  - Includes rollback logic

### 4. DTOs
- ✅ **RequesterDtos.cs**
  - CreateRequesterDto
  - UpdateRequesterDto
  - RequesterDto
  - RequesterProfileDto

- ✅ **ProviderDtos.cs**
  - CreateProviderDto
  - UpdateProviderDto
  - ProviderDto
  - ProviderProfileDto
  - ProviderListDto
  - AvailabilitySlotDto

### 5. Services
- ✅ **IRequesterService.cs** - Interface with 8 methods
- ✅ **RequesterService.cs** - Full implementation
- ✅ **IProviderService.cs** - Interface with 13 methods
- ✅ **ProviderService.cs** - Full implementation

### 6. Documentation
- ✅ **REFACTORED_DUAL_ROLE_GUIDE.md** - Comprehensive guide
- ✅ **This file** - Implementation summary

---

## 📋 What You Need to Do Next

### Phase 1: Database Setup (30 minutes)

```powershell
# Step 1: Open Package Manager Console in Visual Studio
# (Tools > NuGet Package Manager > Package Manager Console)

# Step 2: Generate migration (if not already done)
Add-Migration RefactorDualRoleSchema

# Step 3: Review the migration file at:
# d:\KalyaniMatrimony\Git\HaluluAPI\Migrations\20251115120000_RefactorDualRoleSchema.cs

# Step 4: Apply migration
Update-Database

# Step 5: Verify in SQL
# Connect to your database and run:
SELECT * FROM halulu_api.users LIMIT 1;
SELECT * FROM halulu_api.requesters LIMIT 1;
SELECT * FROM halulu_api.providers LIMIT 1;
```

### Phase 2: Dependency Injection (10 minutes)

Update your **Program.cs** to register the new services:

```csharp
// Add these lines in the services configuration section

// Requester Service
builder.Services.AddScoped<IRequesterService, RequesterService>();

// Provider Service
builder.Services.AddScoped<IProviderService, ProviderService>();
```

### Phase 3: Create Controllers (45 minutes)

#### RequesterController.cs
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequesterController : ControllerBase
{
    private readonly IRequesterService _requesterService;
    private readonly ILogger<RequesterController> _logger;

    public RequesterController(IRequesterService requesterService, ILogger<RequesterController> logger)
    {
        _requesterService = requesterService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateRequesterDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID");

        var result = await _requesterService.RegisterRequester(userGuid, dto);
        return CreatedAtAction(nameof(GetProfile), new { }, result);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID");

        var requester = await _requesterService.GetRequesterByUserId(userGuid);
        if (requester == null)
            return NotFound("Requester profile not found");

        return Ok(requester);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateRequesterDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID");

        var result = await _requesterService.UpdateRequesterProfile(userGuid, dto);
        return Ok(result);
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID");

        var success = await _requesterService.DeactivateRequester(userGuid);
        if (!success)
            return NotFound("Requester profile not found");

        return Ok(new { message = "Requester profile deactivated" });
    }
}
```

#### ProviderController.cs
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProviderController : ControllerBase
{
    private readonly IProviderService _providerService;
    private readonly ILogger<ProviderController> _logger;

    public ProviderController(IProviderService providerService, ILogger<ProviderController> logger)
    {
        _providerService = providerService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateProviderDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID");

        var result = await _providerService.RegisterProvider(userGuid, dto);
        return CreatedAtAction(nameof(GetProfile), new { }, result);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID");

        var provider = await _providerService.GetProviderByUserId(userGuid);
        if (provider == null)
            return NotFound("Provider profile not found");

        return Ok(provider);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProviderDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID");

        var result = await _providerService.UpdateProviderProfile(userGuid, dto);
        return Ok(result);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string? category, [FromQuery] string? serviceArea, [FromQuery] decimal? minRating)
    {
        var providers = await _providerService.SearchProviders(category, serviceArea, minRating);
        return Ok(providers);
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID");

        var success = await _providerService.DeactivateProvider(userGuid);
        if (!success)
            return NotFound("Provider profile not found");

        return Ok(new { message = "Provider profile deactivated" });
    }
}
```

### Phase 4: Update Authentication (20 minutes)

Update **AuthController** to handle role switching:

```csharp
[HttpPost("toggle-role")]
[Authorize]
public async Task<IActionResult> ToggleRole([FromBody] ToggleRoleDto dto)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userId, out var userGuid))
        return Unauthorized("Invalid user ID");

    // Validate requested role
    if (dto.Role != "REQUESTER" && dto.Role != "PROVIDER")
        return BadRequest("Invalid role. Must be REQUESTER or PROVIDER");

    // Check if user has the requested role profile
    bool hasRole = false;
    if (dto.Role == "REQUESTER")
    {
        hasRole = await _requesterService.IsActiveRequester(userGuid);
    }
    else
    {
        hasRole = await _providerService.IsActiveProvider(userGuid);
    }

    if (!hasRole)
        return BadRequest($"User does not have an active {dto.Role} profile");

    // Generate new JWT with updated active_role claim
    var user = await _context.Users.FindAsync(userGuid);
    var token = _jwtService.GenerateToken(user, dto.Role);

    return Ok(new { token, activeRole = dto.Role });
}
```

### Phase 5: Update JWT Service (15 minutes)

Update **JwtService.cs** to include `active_role` claim:

```csharp
public string GenerateToken(User user, string activeRole = "REQUESTER")
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("active_role", activeRole) // Add this line
    };

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
        Issuer = _jwtSettings.Issuer,
        Audience = _jwtSettings.Audience,
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key), 
            SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}
```

### Phase 6: Testing (30 minutes)

#### Test Checklist

```
□ User Registration & Login
  □ Register new user via /api/auth/register
  □ Login via /api/auth/login
  □ Verify JWT token contains user ID and email

□ Requester Flow
  □ POST /api/requester/register → Create requester profile
  □ GET /api/requester/profile → View profile
  □ PUT /api/requester/profile → Update profile
  □ POST /api/requester/deactivate → Deactivate profile

□ Provider Flow
  □ POST /api/provider/register → Create provider profile
  □ GET /api/provider/profile → View profile
  □ PUT /api/provider/profile → Update profile
  □ GET /api/provider/search?category=plumbing → Search providers
  □ POST /api/provider/deactivate → Deactivate profile

□ Role Toggling
  □ POST /api/auth/toggle-role with {"role": "PROVIDER"} → Switch to provider
  □ Verify JWT token updated with active_role = "PROVIDER"
  □ POST /api/auth/toggle-role with {"role": "REQUESTER"} → Switch back
  □ Verify JWT token updated with active_role = "REQUESTER"

□ Data Isolation
  □ Provider cannot see other provider's earnings
  □ Requester cannot see other requester's addresses
  □ Both profiles for same user are separate
```

#### Curl Test Examples

```bash
# Register user
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"fullName":"John Doe","email":"john@example.com","phoneNumber":"1234567890"}'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john@example.com","password":"password123"}'

# Register as requester
curl -X POST http://localhost:5000/api/requester/register \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"address":"123 Main St","preferredCategories":"plumbing,electrical"}'

# Register as provider
curl -X POST http://localhost:5000/api/provider/register \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bio":"Professional plumber with 10 years experience",
    "experienceYears":10,
    "serviceCategories":"plumbing,repairs",
    "serviceAreas":"downtown,suburbs",
    "pricingModel":"per_hour"
  }'

# Toggle to provider role
curl -X POST http://localhost:5000/api/auth/toggle-role \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"role":"PROVIDER"}'
```

---

## 📊 Implementation Order

1. **Apply Migration** (30 min)
   - Run database migration
   - Verify tables exist

2. **Register Services** (10 min)
   - Update Program.cs
   - Register IRequesterService
   - Register IProviderService

3. **Create Controllers** (45 min)
   - RequesterController
   - ProviderController

4. **Update Auth** (20 min)
   - Update JWT to include active_role
   - Add toggle-role endpoint

5. **Test** (30 min)
   - Test all flows
   - Verify data isolation

**Total Time: ~2.5 hours**

---

## 🔍 Important Files to Review

### Before Starting
- [ ] Read: REFACTORED_DUAL_ROLE_GUIDE.md
- [ ] Review: Models/User.cs
- [ ] Review: Models/Requester.cs
- [ ] Review: Models/Provider.cs

### During Implementation
- [ ] DTOs/RequesterDtos.cs
- [ ] DTOs/ProviderDtos.cs
- [ ] Services/IRequesterService.cs
- [ ] Services/RequesterService.cs
- [ ] Services/IProviderService.cs
- [ ] Services/ProviderService.cs

### After Implementation
- [ ] Controllers/RequesterController.cs (create)
- [ ] Controllers/ProviderController.cs (create)
- [ ] Services/AuthService.cs (update for toggle)
- [ ] Services/JwtService.cs (update token generation)

---

## ⚠️ Important Notes

### Data Migration from Old Schema
If you have existing users in the old User table, you need to migrate them:

```csharp
// Create a migration service
public class DataMigrationService
{
    private readonly ApplicationDbContext _context;

    public async Task MigrateExistingUsers()
    {
        var users = await _context.Users.ToListAsync();
        
        foreach (var user in users)
        {
            // Create default requester for every user
            if (!await _context.Requesters.AnyAsync(r => r.UserId == user.Id))
            {
                _context.Requesters.Add(new Requester
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // If user was previously provider, create provider profile
            // (You need to handle this based on your old schema)
        }

        await _context.SaveChangesAsync();
    }
}
```

### Profile Image Handling
- Profile image is now in **User** table only
- Both requester and provider can share it
- To update: Use `/api/user/profile` endpoint

### JSON Fields
- `PreferredCategories` in Requester: Store as comma-separated or JSON array
- `ServiceCategories` in Provider: Store as comma-separated or JSON array
- `Documents` in Provider: Stored as JSON array
- `AvailabilitySlots` in Provider: Stored as JSONB

### About is_logged_in Flag
The guide initially mentioned this but we're using token-based tracking instead:
- Each JWT token represents an active session
- No need to store is_logged_in in database
- For push notifications: Check if user has active token in cache/session store

---

## 🎯 Success Criteria

✅ User can register new account
✅ User can login and get JWT token
✅ User can register as requester
✅ User can register as provider
✅ User can toggle between roles
✅ JWT token includes active_role claim
✅ Requester can't access provider endpoints
✅ Provider can't access requester endpoints
✅ Each user has separate requester/provider profiles
✅ Data is properly isolated by user

---

## 📞 Troubleshooting

### Migration Failed
- Ensure database exists
- Check PostgreSQL is running
- Verify connection string in appsettings.json

### Services Not Injected
- Check Program.cs has AddScoped calls
- Verify namespace imports are correct
- Restart Visual Studio

### JWT Token Missing active_role
- Update JwtService.GenerateToken() method
- Verify claims are being added
- Check token in jwt.io to decode

### Provider Can't Register
- Check if User exists
- Verify no existing Provider profile for that user
- Check phone number is unique

---

## 📄 Quick Reference

### New Tables
```
users (core identity)
├── id (PK)
├── full_name
├── email (unique)
├── phone_number (unique)
├── profile_image
└── relationships: requester, provider

requesters (1-to-1 with users)
├── id (PK)
├── user_id (FK, unique)
├── address
├── preferred_categories
└── timestamps

providers (1-to-1 with users)
├── id (PK)
├── user_id (FK, unique)
├── service_categories
├── experience_years
├── bio
├── service_areas
├── pricing_model
├── documents (JSON)
├── availability_slots (JSONB)
├── rating
├── earnings
└── timestamps
```

### New Endpoints
```
POST   /api/requester/register
GET    /api/requester/profile
PUT    /api/requester/profile
POST   /api/requester/deactivate

POST   /api/provider/register
GET    /api/provider/profile
PUT    /api/provider/profile
GET    /api/provider/search
POST   /api/provider/deactivate

POST   /api/auth/toggle-role
```

---

Generated: 2024-11-15
Version: 1.0