# Refactored Dual-Role Schema Implementation Guide

## 📋 Overview

This guide explains the new simplified database schema that separates user identity from role-specific data. The architecture uses three separate tables instead of mixing concerns in the User table.

---

## 🗂️ Database Schema Structure

### Users Table (Core Identity)
```sql
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
```

**Purpose**: Store core user identity only
**Size**: ~100 bytes per user (minimal)
**Indexes**: Email (unique), PhoneNumber (unique)

---

### Requesters Table
```sql
CREATE TABLE halulu_api.requesters (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL UNIQUE REFERENCES users(id),
    address VARCHAR(500),
    preferred_categories TEXT (JSON array),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);
```

**Purpose**: Store requester-specific data
**When populated**: When user wants to post service requests
**Size**: ~150 bytes per requester

**Fields Explained**:
- `id`: Unique identifier for requester profile
- `user_id`: Links to the user (1-to-1 relationship, unique)
- `address`: Where requester is located
- `preferred_categories`: JSON array of category names they frequently use
- `is_active`: Can be deactivated without deleting

---

### Providers Table
```sql
CREATE TABLE halulu_api.providers (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL UNIQUE REFERENCES users(id),
    service_categories TEXT (JSON array),
    experience_years INTEGER DEFAULT 0,
    bio TEXT (max 1000),
    service_areas TEXT (JSON array),
    pricing_model VARCHAR(100),
    documents TEXT (JSON array),
    availability_slots JSONB,
    rating NUMERIC(3,2) DEFAULT 0,
    earnings NUMERIC(18,2) DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);
```

**Purpose**: Store provider-specific data
**When populated**: When user registers as service provider
**Size**: ~500-800 bytes per provider (larger due to JSON fields)

**Fields Explained**:
- `service_categories`: JSON array `["plumbing", "electrical"]`
- `experience_years`: Years of experience in field
- `bio`: Professional biography
- `service_areas`: JSON array `["downtown", "suburbs"]`
- `pricing_model`: E.g., "per_hour", "per_job", "fixed"
- `documents`: JSON array of certification IDs `["doc_1", "doc_2"]`
- `availability_slots`: JSONB object with weekly schedule
- `rating`: Average rating 0-5 (updated from reviews)
- `earnings`: Lifetime earnings or current balance

---

## 🔄 User Journey & Role Toggle

### Scenario 1: User Registers (Identity Created)
```
User signs up with Email + Phone
    ↓
User record created (minimal data)
    ↓
User can now log in
```

### Scenario 2: User Becomes Requester
```
User clicks "I want to request services"
    ↓
Requester profile created
    ↓
User can now post service requests
```

### Scenario 3: User Becomes Provider
```
User clicks "I want to offer services"
    ↓
Provider registration form opens
    ↓
Provider profile created
    ↓
User can now view job requests & submit bids
```

### Scenario 4: User Switches Roles
```
User has both Requester & Provider profiles
    ↓
UI shows toggle button (Requester ↔ Provider)
    ↓
Click toggle
    ↓
Backend receives request for context switch
    ↓
JWT token updated with active_role claim
    ↓
API endpoints return role-appropriate data
```

---

## 📊 Data Relationships

```
┌─────────────┐
│    Users    │ (Core identity)
│ - id        │
│ - full_name │
│ - email     │
│ - phone     │
│ - profile   │
└──────┬──────┘
       │ 1:1
       ├─────→ ┌──────────────┐
       │       │ Requesters   │ (Role-specific)
       │       │ - id         │
       │       │ - address    │
       │       │ - categories │
       │       └──────────────┘
       │
       └─────→ ┌──────────────┐
               │  Providers   │ (Role-specific)
               │ - id         │
               │ - experience │
               │ - rating     │
               │ - earnings   │
               └──────────────┘
```

---

## 🔐 Authentication & Authorization

### JWT Token Structure
```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "active_role": "REQUESTER",  // or "PROVIDER"
  "has_requester": true,
  "has_provider": true,
  "iat": 1631234567,
  "exp": 1631321000
}
```

### Role Checking in API Endpoints

**Example 1: Check if user is a requester**
```csharp
[HttpPost("api/ServiceRequest/create")]
[Authorize]
public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // Check if user has requester profile
    var requester = await _context.Requesters
        .FirstOrDefaultAsync(r => r.UserId == Guid.Parse(userId) && r.IsActive);
    
    if (requester == null)
        return Unauthorized("User does not have an active Requester profile");
    
    // Create service request...
    return Ok();
}
```

**Example 2: Check if user is a provider**
```csharp
[HttpPost("api/Provider/submitBid")]
[Authorize]
public async Task<IActionResult> SubmitBid([FromBody] SubmitBidDto dto)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    var provider = await _context.Providers
        .FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId) && p.IsActive);
    
    if (provider == null)
        return Unauthorized("User does not have an active Provider profile");
    
    // Submit bid...
    return Ok();
}
```

---

## 📝 API Endpoints by Role

### Requester Endpoints
```
POST   /api/requester/register          → Create requester profile
PUT    /api/requester/profile           → Update requester profile
GET    /api/requester/profile           → Get requester profile
POST   /api/requester/request           → Post service request
GET    /api/requester/requests          → List my requests
PUT    /api/requester/request/{id}      → Update request
DELETE /api/requester/request/{id}      → Cancel request
GET    /api/requester/bids/{requestId}  → View bids on my request
POST   /api/requester/bids/{bidId}/accept → Accept a bid
POST   /api/requester/rating/{providerId}  → Rate provider
```

### Provider Endpoints
```
POST   /api/provider/register           → Create provider profile
PUT    /api/provider/profile            → Update provider profile
GET    /api/provider/profile            → Get provider profile
GET    /api/provider/requests           → List available requests (filtered by categories/areas)
POST   /api/provider/bid                → Submit bid on request
GET    /api/provider/bids               → View my bids
PUT    /api/provider/bids/{bidId}       → Update bid
DELETE /api/provider/bids/{bidId}       → Cancel bid
GET    /api/provider/jobs               → List assigned jobs
PUT    /api/provider/job/{jobId}/start  → Mark job as started
PUT    /api/provider/job/{jobId}/complete → Mark job as complete
GET    /api/provider/reviews            → Get reviews received
```

### Auth Endpoints
```
POST   /api/auth/register               → Register new user (identity only)
POST   /api/auth/login                  → Login and get JWT
POST   /api/auth/toggle-role            → Switch between requester/provider roles
GET    /api/auth/profile                → Get current user profile
POST   /api/auth/logout                 → Logout and clear session
```

---

## 🗄️ Database Migration Steps

### Step 1: Backup Current Database
```sql
-- Create backup (PostgreSQL example)
pg_dump -U username -d halulu_api > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Step 2: Run Migration
```powershell
# In Visual Studio Package Manager Console
Add-Migration RefactorDualRoleSchema

# Review generated migration file

# Apply migration
Update-Database
```

### Step 3: Data Migration (Manual)
If you have existing users, you need to migrate them to the new schema:

```csharp
// Service to migrate existing data
public async Task MigrateExistingUsers()
{
    var users = await _context.Users.ToListAsync();
    
    foreach (var user in users)
    {
        // Create default requester profile for all users
        var requester = new Requester
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Requesters.Add(requester);
        
        // If user was previously a provider, migrate that data
        if (user.Role == UserRole.Provider)
        {
            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ServiceCategories = user.ServiceCategories,
                ExperienceYears = user.ExperienceYears ?? 0,
                Bio = user.Bio,
                ServiceAreas = user.ServiceAreas,
                PricingModel = user.PricingModel,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Providers.Add(provider);
        }
    }
    
    await _context.SaveChangesAsync();
}
```

### Step 4: Verify Migration
```sql
-- Check that all tables exist
SELECT * FROM information_schema.tables 
WHERE table_schema = 'halulu_api'
ORDER BY table_name;

-- Count records
SELECT COUNT(*) FROM halulu_api.users;
SELECT COUNT(*) FROM halulu_api.requesters;
SELECT COUNT(*) FROM halulu_api.providers;

-- Verify relationships
SELECT u.id, r.id as requester_id, p.id as provider_id
FROM halulu_api.users u
LEFT JOIN halulu_api.requesters r ON u.id = r.user_id
LEFT JOIN halulu_api.providers p ON u.id = p.user_id
LIMIT 10;
```

---

## 🔌 Service Implementation

### Requester Service Interface
```csharp
public interface IRequesterService
{
    Task<RequesterDto> RegisterRequester(Guid userId, CreateRequesterDto dto);
    Task<RequesterDto> GetRequesterProfile(Guid userId);
    Task<RequesterDto> UpdateRequesterProfile(Guid userId, UpdateRequesterDto dto);
    Task<bool> DeactivateRequester(Guid userId);
}
```

### Provider Service Interface
```csharp
public interface IProviderService
{
    Task<ProviderDto> RegisterProvider(Guid userId, CreateProviderDto dto);
    Task<ProviderDto> GetProviderProfile(Guid userId);
    Task<ProviderDto> UpdateProviderProfile(Guid userId, UpdateProviderDto dto);
    Task<bool> DeactivateProvider(Guid userId);
    Task UpdateProviderRating(Guid providerId, decimal rating);
    Task UpdateProviderEarnings(Guid providerId, decimal amount);
}
```

---

## 🧪 Testing Checklist

### User Registration & Login
- [ ] Register new user (creates User record)
- [ ] Login returns JWT token
- [ ] JWT contains `sub` (user ID) and `email`

### Requester Flow
- [ ] Register as requester (creates Requester record)
- [ ] Verify one-to-one relationship with User
- [ ] Post service request (creates ServiceRequest linked to User)
- [ ] View my requests (filtered by user_id)
- [ ] Update request details
- [ ] Cancel request

### Provider Flow
- [ ] Register as provider (creates Provider record)
- [ ] Verify one-to-one relationship with User
- [ ] Set availability slots
- [ ] Browse available requests (filtered by categories/areas)
- [ ] Submit bid on request
- [ ] View my bids
- [ ] Update bid amount

### Dual-Role
- [ ] User has both Requester and Provider records
- [ ] Toggle to requester role (JWT claim updated)
- [ ] Toggle to provider role (JWT claim updated)
- [ ] Requester endpoints only accessible with requester role
- [ ] Provider endpoints only accessible with provider role

### Data Isolation
- [ ] Provider cannot view other provider's private data
- [ ] Requester cannot view other requester's addresses
- [ ] Push notifications only to active role

---

## 🚀 Rollout Strategy

### Phase 1: Development (Local)
1. Create new models (Requester.cs, Provider.cs) ✓
2. Update ApplicationDbContext ✓
3. Generate migration file ✓
4. Apply migration to local database
5. Create DTOs
6. Implement services
7. Update controllers
8. Test all flows

### Phase 2: Testing (Staging)
1. Backup production database
2. Create staging database with new schema
3. Run data migration script
4. Test all user flows
5. Load test with sample data
6. Verify push notifications work
7. Get QA sign-off

### Phase 3: Production (Gradual)
1. Announce maintenance window
2. Backup production database
3. Apply migration
4. Run data migration
5. Verify all tables exist and have data
6. Deploy new code
7. Monitor error logs
8. Watch database performance

---

## 📋 Implementation Checklist

### Models & Database
- [x] Create Requester model
- [x] Create Provider model
- [x] Update User model (simplified)
- [x] Update ApplicationDbContext
- [x] Create migration file
- [ ] Run migrations on database
- [ ] Verify schema with `SELECT * FROM information_schema.tables`

### DTOs
- [ ] Create RequesterDto, CreateRequesterDto, UpdateRequesterDto
- [ ] Create ProviderDto, CreateProviderDto, UpdateProviderDto
- [ ] Create RoleToggleDto
- [ ] Create AuthDto with active_role

### Services
- [ ] Implement IRequesterService
- [ ] Implement IProviderService
- [ ] Implement role validation helpers
- [ ] Register services in Dependency Injection

### Controllers
- [ ] Create/Update RequesterController
- [ ] Create/Update ProviderController
- [ ] Update AuthController for role toggle
- [ ] Add role checks to all endpoints

### Authentication
- [ ] Update JWT token generation with active_role claim
- [ ] Update JWT parsing to extract active_role
- [ ] Create role validation middleware
- [ ] Create [Requester], [Provider] authorization attributes

### Business Logic
- [ ] Service request filtering by requester
- [ ] Job filtering by provider categories/areas
- [ ] Bidding system
- [ ] Rating system

### Testing
- [ ] Unit tests for services
- [ ] Integration tests for API endpoints
- [ ] Role-based access control tests
- [ ] Data isolation tests

### Documentation
- [ ] Update API documentation
- [ ] Create migration guide for existing users
- [ ] Document new endpoints
- [ ] Create admin guide for monitoring

---

## 🎯 Key Benefits of This Schema

### ✅ Separation of Concerns
- User table focuses only on identity
- Role-specific data separated into own tables
- Cleaner data model

### ✅ Flexibility
- User can have both roles simultaneously
- Easy to toggle between roles
- Can deactivate roles independently

### ✅ Performance
- Smaller User table = faster queries
- Specific indexes on role tables
- JSONB fields for complex data without normalization overhead

### ✅ Scalability
- Easy to add new roles in future (e.g., Admin, Moderator)
- Each role can have independent growth
- Simpler sharding/partitioning if needed

### ✅ Security
- Role-based queries at database level
- User can't accidentally see other role's data
- Clear authorization boundaries

---

## ⚠️ Common Issues & Fixes

### Issue: "User does not have requester profile"
**Cause**: User exists but no Requester record created
**Fix**: User needs to explicitly "Register as Requester" first

### Issue: JWT token doesn't contain active_role
**Cause**: Login endpoint not updated to include role claim
**Fix**: Update AuthService.GenerateJwtToken() to add active_role claim

### Issue: Profile image not showing
**Cause**: profile_image might still be in requester/provider tables
**Fix**: Verify profile_image is only in users table, update to profile_image column name

### Issue: Provider can see requester's address
**Cause**: Missing authorization check in query
**Fix**: Add `.Where(p => p.UserId == userId)` to all queries

### Issue: ServiceRequest still references old Role enum
**Cause**: ServiceRequest model not updated
**Fix**: ServiceRequest should reference Requester profile instead of User.Role

---

## 📞 Support & Questions

For questions about this architecture:
1. Check the implementation examples in DUAL_ROLE_IMPLEMENTATION_EXAMPLES.md
2. Review API flows in DUAL_ROLE_API_FLOWS.md
3. Check test cases in REFACTORED_SCHEMA_TESTS.md

---

## 📄 Files Reference

**Created/Updated Files:**
- `Models/User.cs` ← Simplified model
- `Models/Requester.cs` ← New model
- `Models/Provider.cs` ← New model
- `Data/ApplicationDbContext.cs` ← Updated with new entities
- `Migrations/20251115120000_RefactorDualRoleSchema.cs` ← Migration file

**Next Steps:**
- Create DTOs in `DTOs/` folder
- Update/Create services in `Services/` folder
- Update controllers in `Controllers/` folder

---

Generated: 2024-11-15
Version: 1.0