# Dual-Role Quick Start Guide

A concise checklist for implementing the dual-role (Requester & Provider) system.

---

## ⚡ 5-Minute Overview

### What is Dual-Role?
A user can be:
- **Requester**: Posts service requests and hires providers
- **Provider**: Offers services and accepts jobs from requesters

### Why?
Marketplace flexibility - users earn as providers when they're not requesting services.

---

## 📋 Implementation Checklist

### STEP 1: Create Models (15 minutes)
- [ ] Create `Models/ProviderService.cs`
- [ ] Create `Models/ProviderAvailability.cs`
- [ ] Create `Models/ServiceBid.cs`
- [ ] Create `Models/Review.cs`
- [ ] Update `ServiceRequest.cs` with provider fields

```csharp
// Add to ServiceRequest.cs:
public Guid? AssignedProviderId { get; set; }
public User? AssignedProvider { get; set; }
public DateTime? AcceptedAt { get; set; }
public DateTime? CompletedAt { get; set; }
public int? ProviderRating { get; set; }
public string? ProviderReview { get; set; }
```

---

### STEP 2: Update Database Context (10 minutes)
- [ ] Add DbSets to `ApplicationDbContext.cs`

```csharp
public DbSet<ProviderService> ProviderServices { get; set; }
public DbSet<ProviderAvailability> ProviderAvailabilities { get; set; }
public DbSet<ServiceBid> ServiceBids { get; set; }
public DbSet<Review> Reviews { get; set; }
```

- [ ] Configure relationships in `OnModelCreating()`
- [ ] Add indexes for performance

---

### STEP 3: Create Migrations (5 minutes)
```powershell
dotnet ef migrations add AddProviderServicesModels
dotnet ef migrations add AddProviderFieldsToServiceRequest
dotnet ef database update
```

---

### STEP 4: Create DTOs (20 minutes)
- [ ] Create `DTOs/ProviderServiceDtos.cs`
  - `CreateProviderServiceDto`
  - `UpdateProviderServiceDto`
  - `ProviderServiceResponseDto`

- [ ] Create `DTOs/ServiceBidDtos.cs`
  - `CreateServiceBidDto`
  - `ServiceBidResponseDto`

- [ ] Create `DTOs/ReviewDtos.cs`
  - `CreateReviewDto`
  - `ReviewResponseDto`

---

### STEP 5: Create Service Interfaces (15 minutes)
- [ ] Create `Services/IProviderService.cs`

Methods needed:
```csharp
// Services Management
CreateProviderServiceAsync()
GetProviderServiceAsync()
GetProviderServicesAsync()
UpdateProviderServiceAsync()
DeleteProviderServiceAsync()

// Availability
SetProviderAvailabilityAsync()
GetProviderAvailabilityAsync()

// Matching
GetAvailableRequestsAsync()
```

---

### STEP 6: Implement Services (45 minutes)
- [ ] Create `Services/ProviderService.cs` (implementation)
- [ ] Update `Services/ServiceRequestService.cs` with role-based filtering

Key method to add:
```csharp
GetDashboardRequestsAsync()
{
    if (user.Role == Requester)
        return requests_CREATED_by_user
    else if (user.Role == Provider)
        return requests_ASSIGNED_to_user
}
```

---

### STEP 7: Create Controllers (30 minutes)
- [ ] Create `Controllers/ProviderServiceController.cs`

Endpoints:
```
POST   /api/ProviderService              # Create service
GET    /api/ProviderService              # Get my services
GET    /api/ProviderService/{id}         # Get one service
PUT    /api/ProviderService/{id}         # Update service

POST   /api/ProviderService/availability # Set hours
GET    /api/ProviderService/availability # Get hours

GET    /api/ProviderService/requests/avail # Get jobs to bid on
```

---

### STEP 8: Update ServiceRequestController (15 minutes)
- [ ] Add role-based logic to `GetUserServiceRequests()`

```csharp
// Route stays: GET /api/ServiceRequest

// But logic changes based on User.Role:
if (user.Role == Requester)
    // Return: WHERE UserId = currentUser
else if (user.Role == Provider)
    // Return: WHERE AssignedProviderId = currentUser
```

---

### STEP 9: Add Authorization Checks (15 minutes)
- [ ] Ensure each endpoint verifies user ownership
- [ ] Return 403 Forbidden for unauthorized access

```csharp
[Authorize]
[HttpPut("ProviderService/{id}")]
public async Task<IActionResult> UpdateService(Guid id, ...)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
    
    // Check if user owns this service
    var service = await _providerService.GetProviderServiceAsync(id, userId);
    if (!service.Success)
        return Forbid("You can only update your own services");
    
    // Proceed...
}
```

---

## 🧪 Testing Each Flow

### Test 1: User as REQUESTER
```bash
1. Register with role=Requester
2. POST /api/ServiceRequest (create request)
3. GET /api/ServiceRequest (see your requests)
4. Receive bids from providers
5. POST /api/ServiceRequest/{id}/accept (accept bid)
6. POST /api/Review (rate provider)
```

### Test 2: User as PROVIDER
```bash
1. Register/update with role=Provider
2. POST /api/ProviderService (register service)
3. POST /api/ProviderService/availability (set hours)
4. GET /api/ProviderService/requests/available (see jobs)
5. POST /api/ServiceRequest/{id}/bid (submit bid)
6. GET /api/ServiceRequest (see assignments)
```

### Test 3: DUAL-ROLE
```bash
1. Register as Requester
2. Create a request (as requester)
3. Update profile to Provider role
4. Register a service (as provider)
5. View available requests to bid on
6. Still able to view your original request
```

---

## 📊 Data Mapping Summary

```
USER (Base)
  ├─ Has many ServiceRequest (as requester)
  └─ Has many ProviderService (as provider)

ServiceRequest
  ├─ UserId → who created request
  ├─ AssignedProviderId → who accepted job
  └─ Reviews → from requester about provider

ProviderService
  ├─ ProviderId → who offers service
  └─ Availability → when they work

ServiceBid
  ├─ RequestId → which request
  └─ ProviderId → who's bidding
```

---

## 🔑 Key Implementation Details

### 1. Role-Based Dashboard
```csharp
// SAME ENDPOINT, DIFFERENT RESPONSES
GET /api/ServiceRequest

// If user.role == Requester:
// ▶ Returns: requests WHERE UserId = me

// If user.role == Provider:
// ▶ Returns: requests WHERE AssignedProviderId = me
```

### 2. Service Availability
```csharp
// Store working hours by day of week
ProviderAvailability
{
  DayOfWeek: Monday,
  StartTime: 09:00,
  EndTime: 18:00
}
```

### 3. Bidding System
```csharp
// Multiple providers can bid on same request
ServiceBid
{
  RequestId: "req-123",
  ProviderId: "provider-A",
  QuoteAmount: 5000,
  Status: "Pending"
}

// Requester accepts ONE bid
ServiceRequest.AssignedProviderId = accepted_bid.ProviderId
```

### 4. Rating System
```csharp
// Only requester can rate provider (after job complete)
Review
{
  ServiceRequestId: "req-123",
  ProviderId: "provider-A",
  ReviewedByUserId: "requester-B",
  Rating: 5,
  Comment: "Great service!"
}

// Update provider's average rating
ProviderService.AverageRating = avg(all_reviews_for_provider)
```

---

## ✅ Pre-Launch Checklist

Database:
- [ ] All 4 new tables created
- [ ] Foreign key constraints set
- [ ] Indexes created for:
  - `ProviderService.ProviderId`
  - `ProviderService.MainCategory`
  - `ServiceBid.RequestId`
  - `ServiceBid.ProviderId`
  - `ServiceRequest.AssignedProviderId`
  - `Review.ProviderId`

API:
- [ ] ProviderServiceController created
- [ ] All endpoints working (test with Postman)
- [ ] Authorization checks on all endpoints
- [ ] Error handling consistent
- [ ] API docs updated

Security:
- [ ] Users can only see own data
- [ ] Users can only update own data
- [ ] Providers can't cancel other's jobs
- [ ] Requesters can't modify provider services
- [ ] JWT validation on all endpoints

Testing:
- [ ] All CRUD operations tested
- [ ] Role-based filtering tested
- [ ] Dual-role scenario tested
- [ ] Edge cases handled (same user as both roles, etc)
- [ ] Authorization failures tested (403 responses)

---

## 🐛 Common Issues & Fixes

### Issue 1: User has role but can't see provider services
**Fix**: Check if role was updated properly
```csharp
// Verify:
var user = await db.Users.FindAsync(userId);
if (user.Role != UserRole.Provider)
    return BadRequest("Not a provider");
```

### Issue 2: Bids not showing for available requests
**Fix**: Check MainCategory matching
```csharp
// Must match exactly (case-sensitive or use .ToLower())
WHERE MainCategory = request.MainCategory
```

### Issue 3: GET /api/ServiceRequest returns wrong data
**Fix**: Check role detection
```csharp
var role = User.FindFirst(ClaimTypes.Role)?.Value;
// OR read from database
var user = await db.Users.FindAsync(userId);
var role = user.Role.ToString();
```

### Issue 4: Authorization returning 403 but user owns resource
**Fix**: Verify ownership comparison
```csharp
// Compare as Guid, not string
if (service.ProviderId != Guid.Parse(userId))
    return Forbid();
```

---

## 📱 API Testing Commands

### 1. Create Provider Service
```bash
curl -X POST http://localhost:5000/api/ProviderService \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "mainCategory": "Cleaning Services",
    "subCategory": "Deep Cleaning",
    "pricePerHour": 500,
    "pricingType": "hourly"
  }'
```

### 2. Get Available Requests
```bash
curl -X GET "http://localhost:5000/api/ProviderService/requests/available?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 3. Submit Bid
```bash
curl -X POST "http://localhost:5000/api/ServiceRequest/{requestId}/bid" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "quoteAmount": 4500,
    "quoteDescription": "Will finish in 4 hours"
  }'
```

### 4. Set Availability
```bash
curl -X POST http://localhost:5000/api/ProviderService/availability \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "dayOfWeek": 1,
    "startTime": "09:00:00",
    "endTime": "18:00:00"
  }'
```

---

## 📖 Full Documentation Files

For detailed information, see:
- `DUAL_ROLE_ARCHITECTURE.md` - Full design & strategy
- `DUAL_ROLE_IMPLEMENTATION_EXAMPLES.md` - Ready-to-use code
- `DUAL_ROLE_API_FLOWS.md` - Visual flows & examples

---

## ⏱️ Time Estimate

| Phase | Tasks | Time |
|-------|-------|------|
| Models | Create 4 models + update ServiceRequest | 15 min |
| Database | DbSets + migrations | 10 min |
| DTOs | Create DTOs | 20 min |
| Services | Interfaces + implementation | 45 min |
| Controllers | ProviderServiceController | 30 min |
| Updates | Update existing controller logic | 15 min |
| Auth | Add authorization checks | 15 min |
| Testing | Manual testing | 30 min |
| **TOTAL** | | **180 min** |

**In practical terms: 3-4 hours for basic implementation**

---

## 🎉 Success Criteria

✅ User can register as Requester  
✅ User can register as Provider  
✅ User can create service requests (as Requester)  
✅ Provider can register services they offer  
✅ Provider can view available requests to bid on  
✅ Provider can submit quotes/bids  
✅ Requester can view bids on their requests  
✅ Requester can accept a bid  
✅ Provider can view assigned jobs  
✅ Requester can rate provider after job  
✅ User can switch roles  
✅ Security: Users can only access their own data  

---

## 🚀 Next Steps After Implementation

1. **Add Payments** - Integrate payment gateway
2. **Add Notifications** - Email/SMS when bids received
3. **Add Chat** - Direct messaging between requester and provider
4. **Add Analytics** - Dashboard with earnings, jobs completed
5. **Add Ratings Algorithm** - Weighted rating system
6. **Add Search/Filter** - Advanced filtering for available jobs

---

**Need help?** Refer to the detailed documentation files or check the example implementations! 🎯