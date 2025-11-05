# Dual-Role Architecture Guide: Requester & Provider Flow

## 📋 Overview

This guide explains how to handle users who can be both **Requesters** (service seekers) and **Providers** (service deliverers) in the HaluluAPI system.

---

## 🎯 Current State

### ✅ What We Have:
- `User` model with `UserRole` enum (Requester, Provider, Admin)
- `ServiceRequest` model - represents requests created by requesters
- Provider fields already in User model: `ServiceCategories`, `ExperienceYears`, `Bio`, `ServiceAreas`, `PricingModel`

### ❌ What's Missing:
- Provider service offerings/inventory model
- Distinction between requester vs provider data
- Provider-specific endpoints to manage their services
- Matching logic between requests and provider services

---

## 🏗️ Architecture Design

### Two-Stream Data Model

```
USER (Dual Role Capable)
├── AS REQUESTER → Creates ServiceRequests
│   └── ServiceRequest (what they need)
│       ├── BookingType: book_now, schedule_later, get_quote
│       ├── MainCategory, SubCategory
│       ├── Date, Time, Location
│       └── Status: Pending → Confirmed → Completed
│
└── AS PROVIDER → Manages ProviderServices
    └── ProviderService (what they offer)
        ├── ServiceCategories (multiple)
        ├── PricingModel
        ├── Availability (hours/days)
        ├── ServiceAreas (locations they serve)
        ├── ExperienceYears
        └── Rating/Reviews
```

---

## 📦 New Models Required

### 1. **ProviderService Model**
Represents services offered by a provider

```csharp
public class ProviderService
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProviderId { get; set; }  // User who offers the service

    [Required]
    public string MainCategory { get; set; }  // e.g., "Cleaning Services"

    [Required]
    public string SubCategory { get; set; }  // e.g., "Deep Cleaning"

    public string? Description { get; set; }

    [Required]
    public decimal PricePerHour { get; set; }  // or fixed price

    public string? PricingType { get; set; }  // "hourly", "fixed", "negotiable"

    public string? ServiceAreas { get; set; }  // JSON array of locations

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User? Provider { get; set; }
}
```

### 2. **ServiceRequest Model** (Enhanced)
Add optional provider assignment

```csharp
// Add these fields to existing ServiceRequest model:

public Guid? AssignedProviderId { get; set; }  // Provider who accepted the job

public string? ProviderQuote { get; set; }  // Provider's quote for get_quote requests

public DateTime? AcceptedAt { get; set; }

public DateTime? CompletedAt { get; set; }

public string? CancellationReason { get; set; }

public int? ProviderRating { get; set; }  // 1-5 stars

public string? ProviderReview { get; set; }

// Navigation
public User? AssignedProvider { get; set; }

public ProviderService? ProviderService { get; set; }
```

### 3. **ProviderAvailability Model** (Optional but Recommended)
Track provider's available hours

```csharp
public class ProviderAvailability
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }  // Monday, Tuesday, etc.

    [Required]
    public TimeSpan StartTime { get; set; }  // 09:00:00

    [Required]
    public TimeSpan EndTime { get; set; }  // 18:00:00

    public bool IsAvailable { get; set; } = true;

    // Navigation
    public User? Provider { get; set; }
}
```

---

## 🔄 API Endpoint Structure

### **FOR REQUESTERS** (Creating & Managing Service Requests)

```
POST   /api/ServiceRequest                    # Create new request
GET    /api/ServiceRequest                    # Get my requests (as requester)
GET    /api/ServiceRequest/{id}               # Get single request details
PUT    /api/ServiceRequest/{id}               # Update my request
DELETE /api/ServiceRequest/{id}/cancel        # Cancel my request
```

### **FOR PROVIDERS** (Managing Services Offered)

```
POST   /api/ProviderService                   # Register new service
GET    /api/ProviderService                   # Get my services (as provider)
GET    /api/ProviderService/{id}              # Get service details
PUT    /api/ProviderService/{id}              # Update service info
DELETE /api/ProviderService/{id}              # Remove service offering

# Availability Management
POST   /api/ProviderService/availability      # Set availability hours
GET    /api/ProviderService/availability      # Get my availability
PUT    /api/ProviderService/availability/{id} # Update availability
```

### **FOR MATCHING & BIDDING**

```
GET    /api/ServiceRequest/available          # Get requests matching my services (provider view)
POST   /api/ServiceRequest/{requestId}/bid    # Provider submits quote/bid
GET    /api/ServiceRequest/{requestId}/bids   # Get all bids for request
POST   /api/ServiceRequest/{requestId}/accept # Accept a bid

GET    /api/ServiceRequest/{requestId}/quotes # Get quotes (requester view)
```

### **FOR REVIEWS & RATINGS**

```
POST   /api/Review                            # Submit review after completion
GET    /api/Review/user/{userId}              # Get provider ratings
```

---

## 🔐 Role-Based Access Control

### **Data Access Patterns**

```csharp
// Controller pseudocode for GetUserServiceRequests
[Authorize]
public async Task<IActionResult> GetUserServiceRequests()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
    var user = await _dbContext.Users.FindAsync(userId);

    if (user.Role == UserRole.Requester)
    {
        // Return requests CREATED BY this user
        var requests = _dbContext.ServiceRequests
            .Where(r => r.UserId == userId)
            .ToList();
        return Ok(requests);
    }
    else if (user.Role == UserRole.Provider)
    {
        // Return requests ASSIGNED TO this provider
        var assignments = _dbContext.ServiceRequests
            .Where(r => r.AssignedProviderId == userId)
            .ToList();
        return Ok(assignments);
    }
}
```

---

## 📊 Data Flow Examples

### Example 1: Dual-Role User as REQUESTER

```
1. User registers → Role: "Dual" or creates with Role: "Requester"
2. User creates ServiceRequest (POST /api/ServiceRequest)
   - Stored in ServiceRequest table with UserId = UserA
   - Status: Pending
3. Other providers can see it and submit bids
4. User selects a provider
5. AssignedProviderId is set to selected provider
6. Status: Confirmed
```

### Example 2: Dual-Role User as PROVIDER

```
1. User updates profile → Sets Role: "Provider"
2. User registers services (POST /api/ProviderService)
   - Stored in ProviderService table with ProviderId = UserA
3. User sets availability (POST /api/ProviderService/availability)
4. User views available requests (GET /api/ServiceRequest/available)
5. User submits quote (POST /api/ServiceRequest/{id}/bid)
6. Requester accepts → AssignedProviderId = UserA
7. Complete job → Update status to Completed
```

---

## 🗄️ Database Schema

### Enhanced ApplicationDbContext

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<ServiceRequest> ServiceRequests { get; set; }
    
    // NEW
    public DbSet<ProviderService> ProviderServices { get; set; }
    public DbSet<ProviderAvailability> ProviderAvailabilities { get; set; }
    public DbSet<ServiceBid> ServiceBids { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Relationships
        modelBuilder.Entity<ProviderService>()
            .HasOne(ps => ps.Provider)
            .WithMany()
            .HasForeignKey(ps => ps.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.AssignedProvider)
            .WithMany()
            .HasForeignKey(sr => sr.AssignedProviderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for performance
        modelBuilder.Entity<ProviderService>()
            .HasIndex(ps => ps.ProviderId);

        modelBuilder.Entity<ServiceRequest>()
            .HasIndex(sr => sr.AssignedProviderId);

        modelBuilder.Entity<ServiceRequest>()
            .HasIndex(sr => sr.MainCategory);
    }
}
```

---

## 📝 Migration Strategy

### Step 1: Create Migration for New Tables

```powershell
dotnet ef migrations add AddProviderServicesAndBiddingTables
dotnet ef database update
```

### Step 2: Add Fields to ServiceRequest

```powershell
dotnet ef migrations add AddProviderAssignmentToServiceRequest
dotnet ef database update
```

---

## 🎯 Implementation Checklist

### Phase 1: Models & Database
- [ ] Create `ProviderService` model
- [ ] Create `ProviderAvailability` model  
- [ ] Update `ServiceRequest` model with provider fields
- [ ] Create migrations and apply

### Phase 2: Services Layer
- [ ] Create `IProviderService` interface
- [ ] Create `ProviderService` implementation
- [ ] Add provider-related methods to `IServiceRequestService`
- [ ] Add filtering logic for provider vs requester

### Phase 3: Controllers
- [ ] Create `ProviderServiceController`
- [ ] Update `ServiceRequestController` with role-based logic
- [ ] Add endpoints for bidding/matching

### Phase 4: DTOs
- [ ] Create `ProviderServiceDto`, `CreateProviderServiceDto`
- [ ] Create `ServiceBidDto`, `CreateBidDto`
- [ ] Update `ServiceRequestResponseDto` with provider info
- [ ] Create `ReviewDto`

### Phase 5: Business Logic
- [ ] Implement service matching algorithm
- [ ] Implement bid management
- [ ] Implement rating/review system
- [ ] Add role-based filtering

---

## 🔒 Security Considerations

### Authorization Rules

```
✅ User can only:
   - View/edit their own requests (as requester)
   - View/edit their own provider services
   - View/edit their own bids
   - Accept bids on their own requests
   - Rate providers who served them

❌ User cannot:
   - View other user's requests
   - Edit other user's requests
   - Accept requests assigned to others
   - Update provider services they don't own
```

### Example Authorization

```csharp
[Authorize]
[HttpPut("ProviderService/{id}")]
public async Task<IActionResult> UpdateProviderService(Guid id, UpdateProviderServiceDto dto)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
    var service = await _dbContext.ProviderServices.FindAsync(id);
    
    // Security check
    if (service.ProviderId != Guid.Parse(userId))
        return Forbid("You can only update your own services");
    
    // Update logic...
}
```

---

## 📊 Example API Responses

### Get My Requests (As Requester)
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "userId": "requester-guid",
      "mainCategory": "Cleaning Services",
      "status": "Pending",
      "createdAt": "2025-11-01T10:00:00Z",
      "assignedProviderId": null,
      "bids": [
        {
          "providerId": "provider-guid",
          "providerName": "John's Cleaning",
          "quote": 5000,
          "rating": 4.8,
          "submittedAt": "2025-11-01T11:00:00Z"
        }
      ]
    }
  ],
  "role": "Requester"
}
```

### Get My Assignments (As Provider)
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "requesterName": "Sarah Johnson",
      "mainCategory": "Cleaning Services",
      "status": "Confirmed",
      "assignedAt": "2025-11-01T12:00:00Z",
      "requesterRating": null
    }
  ],
  "role": "Provider"
}
```

---

## 🚀 Recommended Implementation Order

1. **Start Simple**: Handle basic dual-role switching
2. **Add Matching**: Simple category-based matching
3. **Add Bidding**: Quote/bid submission
4. **Add Ratings**: Review system
5. **Add Availability**: Calendar/availability management

---

## 🔗 Related Files to Update

```
Models/
  └─ ServiceRequest.cs          (add AssignedProviderId, rating fields)
  └─ ProviderService.cs         (NEW)
  └─ ProviderAvailability.cs    (NEW)
  
Services/
  └─ ServiceRequestService.cs   (add role-based filtering)
  └─ IProviderService.cs        (NEW interface)
  └─ ProviderService.cs         (NEW implementation)
  
Controllers/
  └─ ServiceRequestController.cs (update with role checks)
  └─ ProviderServiceController.cs (NEW)
  
DTOs/
  └─ ServiceRequestDtos.cs      (update response with provider info)
  └─ ProviderServiceDtos.cs     (NEW)
  
Data/
  └─ ApplicationDbContext.cs    (add DbSets, relationships)
```

---

## 💡 Quick Reference

| Operation | User Role | Endpoint | Data |
|-----------|-----------|----------|------|
| Create Request | Requester | POST /api/ServiceRequest | ServiceRequest + UserId |
| Get My Requests | Requester | GET /api/ServiceRequest | ServiceRequest where UserId=me |
| Get My Services | Provider | GET /api/ProviderService | ProviderService where ProviderId=me |
| Get Available Requests | Provider | GET /api/ServiceRequest/available | ServiceRequest matching my services |
| Submit Bid | Provider | POST /api/ServiceRequest/{id}/bid | Quote + ProviderId + RequestId |
| Accept Bid | Requester | POST /api/ServiceRequest/{id}/accept | Set AssignedProviderId |
| View My Assignments | Provider | GET /api/ServiceRequest | ServiceRequest where AssignedProviderId=me |
| Rate Provider | Requester | POST /api/Review | Rating + Review + ProviderId |

---

## ✅ Benefits of This Architecture

✅ Clean separation between requester and provider data  
✅ Scalable - easy to add marketplace features  
✅ Flexible - users can switch roles easily  
✅ Audit trail - all actions logged with timestamps  
✅ Security - role-based access control built-in  
✅ Performance - indexed queries for common searches