# Refactored Schema - Quick Start Checklist

## 🚀 5-Minute Overview

**Old Schema Problem**: User table had mixed responsibilities (identity + provider data)
**New Schema Solution**: Separate tables for identity, requester role, and provider role

```
Before:  Users table (mixed data - messy)
After:   Users table + Requesters table + Providers table (clean separation)
```

**Why**: Each role has different data; separation makes it cleaner and faster.

---

## ⚙️ Implementation Steps

### Step 1: Apply Database Migration ✓ (READY TO RUN)
```powershell
# In Visual Studio Package Manager Console
Add-Migration RefactorDualRoleSchema
Update-Database
```

**What it does**:
- Creates `requesters` table
- Creates `providers` table  
- Simplifies `users` table
- Adds indexes for performance

**Files involved**:
- `Migrations/20251115120000_RefactorDualRoleSchema.cs` ← Ready to use

---

### Step 2: Register Services (COPY-PASTE READY)
Edit `Program.cs` and add these lines in the services section:

```csharp
// Around line 30-40, after other service registrations
builder.Services.AddScoped<IRequesterService, RequesterService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
```

**Files involved**:
- `Services/IRequesterService.cs` ← Interface done
- `Services/RequesterService.cs` ← Implementation done
- `Services/IProviderService.cs` ← Interface done
- `Services/ProviderService.cs` ← Implementation done

---

### Step 3: Create Controllers (COPY-PASTE READY)

#### Create `Controllers/RequesterController.cs`
```csharp
using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HaluluAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequesterController : ControllerBase
{
    private readonly IRequesterService _requesterService;

    public RequesterController(IRequesterService requesterService)
    {
        _requesterService = requesterService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateRequesterDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user");

        var result = await _requesterService.RegisterRequester(userGuid, dto);
        return Ok(result);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user");

        var result = await _requesterService.GetRequesterByUserId(userGuid);
        if (result == null)
            return NotFound("Profile not found");

        return Ok(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateRequesterDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user");

        var result = await _requesterService.UpdateRequesterProfile(userGuid, dto);
        return Ok(result);
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user");

        await _requesterService.DeactivateRequester(userGuid);
        return Ok(new { message = "Requester deactivated" });
    }
}
```

#### Create `Controllers/ProviderController.cs`
```csharp
using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HaluluAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProviderController : ControllerBase
{
    private readonly IProviderService _providerService;

    public ProviderController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateProviderDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user");

        var result = await _providerService.RegisterProvider(userGuid, dto);
        return Ok(result);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user");

        var result = await _providerService.GetProviderByUserId(userGuid);
        if (result == null)
            return NotFound("Profile not found");

        return Ok(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProviderDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user");

        var result = await _providerService.UpdateProviderProfile(userGuid, dto);
        return Ok(result);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? category = null,
        [FromQuery] string? serviceArea = null,
        [FromQuery] decimal? minRating = null)
    {
        var providers = await _providerService.SearchProviders(category, serviceArea, minRating);
        return Ok(providers);
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user");

        await _providerService.DeactivateProvider(userGuid);
        return Ok(new { message = "Provider deactivated" });
    }
}
```

**Files created**:
- `Controllers/RequesterController.cs` ← Create this
- `Controllers/ProviderController.cs` ← Create this

---

### Step 4: Update JWT Service (10 MINUTES)

Edit `Services/JwtService.cs` and update the `GenerateToken` method to include `active_role` claim:

**Find this**:
```csharp
public string GenerateToken(User user)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email)
    };
    // ... rest of method
}
```

**Replace with this**:
```csharp
public string GenerateToken(User user, string activeRole = "REQUESTER")
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("active_role", activeRole)  // ADD THIS LINE
    };
    // ... rest of method remains same
}
```

---

### Step 5: Test (15 MINUTES)

#### Test 1: Register and Login
```bash
# 1. Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "email": "john@test.com",
    "phoneNumber": "9876543210"
  }'

# Expected: User created
# Note: Store the user ID returned

# 2. Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@test.com",
    "password": "password123"
  }'

# Expected: JWT token
# Note: Store the token
```

#### Test 2: Create Requester Profile
```bash
TOKEN="YOUR_JWT_TOKEN_HERE"

curl -X POST http://localhost:5000/api/requester/register \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "address": "123 Main Street, City",
    "preferredCategories": "plumbing,electrical"
  }'

# Expected: Requester profile created
```

#### Test 3: Create Provider Profile
```bash
TOKEN="YOUR_JWT_TOKEN_HERE"

curl -X POST http://localhost:5000/api/provider/register \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bio": "Professional plumber with 10 years experience",
    "experienceYears": 10,
    "serviceCategories": "plumbing,repairs",
    "serviceAreas": "downtown,suburbs",
    "pricingModel": "per_hour"
  }'

# Expected: Provider profile created
```

#### Test 4: Get Both Profiles
```bash
TOKEN="YOUR_JWT_TOKEN_HERE"

# Get requester profile
curl -X GET http://localhost:5000/api/requester/profile \
  -H "Authorization: Bearer $TOKEN"

# Get provider profile
curl -X GET http://localhost:5000/api/provider/profile \
  -H "Authorization: Bearer $TOKEN"

# Expected: Both profiles returned
```

---

## 📋 Files Overview

### Created Files ✅
| File | Type | Status | Purpose |
|------|------|--------|---------|
| Models/Requester.cs | Model | ✅ Done | Requester data structure |
| Models/Provider.cs | Model | ✅ Done | Provider data structure |
| DTOs/RequesterDtos.cs | DTO | ✅ Done | Requester request/response |
| DTOs/ProviderDtos.cs | DTO | ✅ Done | Provider request/response |
| Services/IRequesterService.cs | Interface | ✅ Done | Requester service contract |
| Services/RequesterService.cs | Service | ✅ Done | Requester implementation |
| Services/IProviderService.cs | Interface | ✅ Done | Provider service contract |
| Services/ProviderService.cs | Service | ✅ Done | Provider implementation |
| Migrations/2025*_RefactorDualRoleSchema.cs | Migration | ✅ Done | Database schema change |

### Updated Files ✅
| File | Changes |
|------|---------|
| Models/User.cs | Simplified to core fields only |
| Data/ApplicationDbContext.cs | Added Requester, Provider DbSets |
| Program.cs | Need to add service registrations |
| Services/JwtService.cs | Need to add active_role claim |

### To Create 📝
| File | Type | Deadline |
|------|------|----------|
| Controllers/RequesterController.cs | Controller | This sprint |
| Controllers/ProviderController.cs | Controller | This sprint |

---

## ✅ Completion Checklist

### Database
- [ ] Run migration: `Update-Database`
- [ ] Verify tables exist: `requesters`, `providers`, `users`
- [ ] Check indexes created

### Code
- [ ] Register services in Program.cs
- [ ] Create RequesterController
- [ ] Create ProviderController
- [ ] Update JwtService with active_role

### Testing
- [ ] Test user registration
- [ ] Test requester profile creation
- [ ] Test provider profile creation
- [ ] Test getting profiles
- [ ] Test updating profiles
- [ ] Verify JWT token includes active_role

### Documentation
- [ ] Read REFACTORED_DUAL_ROLE_GUIDE.md
- [ ] Update API documentation
- [ ] Document endpoints in README

---

## 🎯 Expected Endpoints After Completion

```
AUTH:
POST   /api/auth/register           → Create user account
POST   /api/auth/login              → Login and get JWT
POST   /api/auth/logout             → Logout
POST   /api/auth/toggle-role        → Switch roles (optional for now)

REQUESTER:
POST   /api/requester/register      → Register as requester
GET    /api/requester/profile       → Get requester profile
PUT    /api/requester/profile       → Update profile
POST   /api/requester/deactivate    → Deactivate role

PROVIDER:
POST   /api/provider/register       → Register as provider
GET    /api/provider/profile        → Get provider profile
PUT    /api/provider/profile        → Update profile
GET    /api/provider/search         → Search providers
POST   /api/provider/deactivate     → Deactivate role
```

---

## ⚡ Time Estimates

| Step | Time | Status |
|------|------|--------|
| Run migration | 5 min | Ready |
| Register services | 5 min | Ready |
| Create controllers | 20 min | Template ready |
| Update JWT | 10 min | Ready |
| Test endpoints | 15 min | Ready |
| **TOTAL** | **55 min** | ✅ |

---

## 🆘 Quick Troubleshooting

| Error | Solution |
|-------|----------|
| "Migration failed" | Run `Update-Database` in Package Manager Console |
| "Service not registered" | Add lines to Program.cs services section |
| "JWT missing active_role" | Update JwtService.GenerateToken() method |
| "Requester not found" | User must call POST /api/requester/register first |
| "Provider not found" | User must call POST /api/provider/register first |
| "401 Unauthorized" | JWT token missing or expired |
| "403 Forbidden" | User role doesn't match endpoint requirements |

---

## 📊 Database Schema (Quick Reference)

```sql
-- Users (core identity)
CREATE TABLE halulu_api.users (
    id UUID PRIMARY KEY,
    full_name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    phone_number VARCHAR(20) NOT NULL UNIQUE,
    profile_image VARCHAR(500),
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP,
    last_login_at TIMESTAMP
);

-- Requesters (role-specific)
CREATE TABLE halulu_api.requesters (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL UNIQUE REFERENCES users(id),
    address VARCHAR(500),
    preferred_categories TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);

-- Providers (role-specific)
CREATE TABLE halulu_api.providers (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL UNIQUE REFERENCES users(id),
    service_categories TEXT,
    experience_years INTEGER DEFAULT 0,
    bio VARCHAR(1000),
    service_areas TEXT,
    pricing_model VARCHAR(100),
    documents TEXT,
    availability_slots JSONB,
    rating NUMERIC(3,2) DEFAULT 0,
    earnings NUMERIC(18,2) DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);
```

---

## 📞 Questions?

Refer to: `REFACTORED_DUAL_ROLE_GUIDE.md` for detailed explanation

---

**Status**: Ready to implement
**Last Updated**: 2024-11-15
**Estimated Implementation Time**: 1 hour
