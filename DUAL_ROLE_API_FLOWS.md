# Dual-Role API Flows & Visual Guide

## 🏗️ Database Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                    USER                                      │
│  ┌──────────────┬──────────────┬──────────────┬──────────────────────────┐  │
│  │ Id (PK)      │ Role         │ Email        │ Phone                    │  │
│  │ UniqueUserId │ FirstName    │ LastName     │ ProfileImageUrl          │  │
│  │ CreatedAt    │ IsActive     │ IsProfileComplete                       │  │
│  │              │                                                        │  │
│  │ [Provider Fields]                                                    │  │
│  │ - ServiceCategories                                                  │  │
│  │ - ExperienceYears                                                    │  │
│  │ - Bio                                                                │  │
│  │ - ServiceAreas                                                       │  │
│  │ - PricingModel                                                       │  │
│  └──────────────┴──────────────┴──────────────┴──────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
           ▲                                    ▲                    ▲
           │ 1                                  │ 1                  │ 1
           │                                    │                    │
           │                                    │                    │
      ┌────┴──────────────┐          ┌─────────┴──────┐       ┌─────┴──────────┐
      │ ServiceRequest    │          │ ProviderService│       │ ProviderAvail  │
      │ (Requester Data)  │          │ (Provider Data)│       │ (Availability) │
      ├────────────────┬──┤          ├─────────────┬──┤       ├────────────┬───┤
      │ Id (PK)        │  │          │ Id (PK)     │  │       │ Id (PK)    │   │
      │ UserId (FK)    │  │          │ ProviderId  │  │       │ ProviderId │   │
      │ BookingType    │  │          │ (FK)        │  │       │ (FK)       │   │
      │ MainCategory   │  │          │             │  │       │            │   │
      │ SubCategory    │  │          │ MainCategory│  │       │ DayOfWeek  │   │
      │ Date           │  │          │ SubCategory │  │       │ StartTime  │   │
      │ Time           │  │          │             │  │       │ EndTime    │   │
      │ Location       │  │          │ PricePerHour│  │       │            │   │
      │ Status         │  │          │ PricingType │  │       │ IsAvailable│   │
      │ Notes          │  │          │             │  │       │            │   │
      │ CreatedAt      │  │          │ Description │  │       │ CreatedAt  │   │
      │ UpdatedAt      │  │          │             │  │       └────────────┴───┘
      │                │  │          │ IsActive    │  │
      │ [NEW FIELDS]   │  │          │ Rating      │  │
      │ AssignedProv.  │  │          │ Reviews     │  │
      │ ProviderQuote  │  │          │             │  │
      │ AcceptedAt     │◄─┼──────────┤ CreatedAt   │  │
      │ CompletedAt    │  │          │ UpdatedAt   │  │
      │ Rating         │  │          └─────────────┴──┘
      │ Review         │  │                ▲
      └────────────────┴──┘                │ 1
                 ▲                         │
                 │ 1                       │
                 │                    ┌────┴──────────┐
                 │                    │  ServiceBid   │
                 │                    ├──────────────┤
                 │                    │ Id (PK)      │
            ┌────┴─────────────────┐  │ RequestId(FK)│
            │      Review          │  │ ProviderId(FK
            ├──────────────────────┤  │              │
            │ Id (PK)              │  │ QuoteAmount  │
            │ ServiceRequestId(FK) │  │ Description  │
            │ ProviderId(FK)       │  │ Status       │
            │ ReviewedByUserId(FK) │  │              │
            │ Rating               │  │ CreatedAt    │
            │ Comment              │  │ UpdatedAt    │
            │ CreatedAt            │  └──────────────┘
            └──────────────────────┘
```

---

## 📊 User Role Decision Tree

```
                            ┌─ USER REGISTRATION ─┐
                            │                     │
                    ┌───────┴─────────┬──────────┐
                    │                 │          │
              SELECT ROLE         REQUESTER   PROVIDER
                    │                 │          │
        ┌───────────┼────────┐        │          │
        │           │        │        │          │
    REQUESTER    PROVIDER  ADMIN      │          │
        │           │        │        │          │
        └────┬──────┘        │        │          │
             │               │        │          │
        FILL PROFILE      ──────  FILL PROFILE + 
             │           CAN DO   SERVICE INFO
             │               │        │
        CREATE            MANAGE   MANAGE
        REQUESTS           ADMIN    SERVICES
             │              PANEL       │
             ▼                          ▼
        ┌──────────────────────────────────────┐
        │   CAN USER BE BOTH REQUESTER & PROVIDER?    │
        │                                             │
        │   ✅ YES - After registration, user can    │
        │           update role via profile update    │
        │                                             │
        │   Strategy: Allow role switching via       │
        │   PATCH /api/Auth/profile                   │
        └──────────────────────────────────────────────┘
```

---

## 🔄 Data Flow: User as REQUESTER

```
Step 1: User Registration
┌────────────────────────────────┐
│ POST /api/Auth/register        │
│ - Email                        │
│ - Phone                        │
│ - Password                     │
│ - Role: "Requester"            │
└────────────┬───────────────────┘
             ▼
┌────────────────────────────────┐
│ Create User with Role=Requester│
│ ServiceCategories = null       │
└────────────┬───────────────────┘
             ▼
Step 2: Create Service Request
┌────────────────────────────────────────┐
│ POST /api/ServiceRequest               │
│ {                                      │
│   "bookingType": "book_now",           │
│   "mainCategory": "Cleaning",          │
│   "subCategory": "Deep Cleaning",      │
│   "date": "2025-11-10",                │
│   "location": "123 Main St",           │
│   "notes": "Please bring supplies"     │
│ }                                      │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ INSERT INTO ServiceRequest             │
│ - Id: GUID                             │
│ - UserId: Requester_ID                 │
│ - Status: Pending                      │
│ - CreatedAt: NOW()                     │
│ - AssignedProviderId: NULL             │
└────────────┬───────────────────────────┘
             ▼
Step 3: View Requests (Requester View)
┌────────────────────────────────────────┐
│ GET /api/ServiceRequest                │
│ [Automatic: Extract UserId from JWT]   │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ SELECT * FROM ServiceRequest           │
│ WHERE UserId = Current_User_ID         │
│ (Returns all requests created by user) │
└────────────┬───────────────────────────┘
             ▼
Step 4: Accept Provider Bid
┌────────────────────────────────────────┐
│ POST /api/ServiceRequest/{id}/accept   │
│ {                                      │
│   "bidId": "bid-guid"                  │
│ }                                      │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ UPDATE ServiceRequest                  │
│ SET AssignedProviderId = ProviderId    │
│     AcceptedAt = NOW()                 │
│     Status = "Confirmed"               │
│ WHERE Id = RequestId                   │
└────────────┬───────────────────────────┘
             ▼
Step 5: Rate Provider (After Completion)
┌────────────────────────────────────────┐
│ POST /api/Review                       │
│ {                                      │
│   "serviceRequestId": "req-id",        │
│   "rating": 5,                         │
│   "comment": "Excellent service!"      │
│ }                                      │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ INSERT INTO Review                     │
│ + UPDATE ServiceRequest                │
│   SET ProviderRating = 5               │
│       ProviderReview = comment         │
└────────────────────────────────────────┘
```

---

## 🔄 Data Flow: User as PROVIDER

```
Step 1: Update User Profile to Provider
┌────────────────────────────────────────┐
│ PATCH /api/Auth/profile                │
│ {                                      │
│   "role": "Provider",                  │
│   "serviceCategories": "Cleaning",     │
│   "experienceYears": 5,                │
│   "bio": "Professional cleaner",       │
│   "serviceAreas": "[\"Mumbai\"]",      │
│   "pricingModel": "hourly"             │
│ }                                      │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ UPDATE User SET                        │
│   Role = "Provider"                    │
│   ServiceCategories = "Cleaning"       │
│   ExperienceYears = 5                  │
│   ... (other provider fields)          │
└────────────┬───────────────────────────┘
             ▼
Step 2: Register Service Offerings
┌────────────────────────────────────────┐
│ POST /api/ProviderService              │
│ {                                      │
│   "mainCategory": "Cleaning Services", │
│   "subCategory": "Deep Cleaning",      │
│   "description": "Pro deep clean",     │
│   "pricePerHour": 500.00,              │
│   "pricingType": "hourly",             │
│   "serviceAreas": "[\"Mumbai\"]"       │
│ }                                      │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ INSERT INTO ProviderService            │
│ - Id: GUID                             │
│ - ProviderId: Provider_ID              │
│ - MainCategory: "Cleaning Services"    │
│ - IsActive: true                       │
└────────────┬───────────────────────────┘
             ▼
Step 3: Set Availability
┌────────────────────────────────────────┐
│ POST /api/ProviderService/availability │
│ {                                      │
│   "dayOfWeek": 1,      # Monday        │
│   "startTime": "09:00",                │
│   "endTime": "18:00"                   │
│ }                                      │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ INSERT INTO ProviderAvailability       │
│ - ProviderId: Provider_ID              │
│ - DayOfWeek: Monday                    │
│ - StartTime: 09:00                     │
│ - EndTime: 18:00                       │
└────────────┬───────────────────────────┘
             ▼
Step 4: View Available Requests
┌────────────────────────────────────────┐
│ GET /api/ProviderService/requests/avail│
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ SELECT * FROM ServiceRequest           │
│ WHERE MainCategory IN                  │
│   (SELECT MainCategory                 │
│    FROM ProviderService                │
│    WHERE ProviderId = Current_Provider)│
│ AND Status = "Pending"                 │
│ AND AssignedProviderId IS NULL         │
│ (Returns all available requests)       │
└────────────┬───────────────────────────┘
             ▼
Step 5: Submit Bid/Quote
┌────────────────────────────────────────┐
│ POST /api/ServiceRequest/{id}/bid      │
│ {                                      │
│   "quoteAmount": 5000,                 │
│   "quoteDescription": "Will take 4hrs" │
│ }                                      │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ INSERT INTO ServiceBid                 │
│ - RequestId: Service_Request_ID        │
│ - ProviderId: Current_Provider         │
│ - QuoteAmount: 5000                    │
│ - Status: "Pending"                    │
└────────────┬───────────────────────────┘
             ▼
Step 6: View My Assignments
┌────────────────────────────────────────┐
│ GET /api/ServiceRequest                │
│ [Provider Role Check]                  │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ SELECT * FROM ServiceRequest           │
│ WHERE AssignedProviderId = Provider_ID │
│ (Returns all assigned requests)        │
└────────────┬───────────────────────────┘
             ▼
Step 7: Complete Job & Update Status
┌────────────────────────────────────────┐
│ PUT /api/ServiceRequest/{id}           │
│ {                                      │
│   "status": "Completed"                │
│ }                                      │
└────────────┬───────────────────────────┘
             ▼
┌────────────────────────────────────────┐
│ UPDATE ServiceRequest SET              │
│   Status = "Completed"                 │
│   CompletedAt = NOW()                  │
└────────────────────────────────────────┘
```

---

## 🔄 Data Flow: Dual-Role User Switching

```
                    ┌──────────────────────┐
                    │   User Registered    │
                    │                      │
                    │ Email: john@ex.com   │
                    │ Role: Requester      │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │ CREATE REQUESTS      │
                    │                      │
                    │ ServiceRequest(s)    │
                    │ UserId = john_id     │
                    │ Status = Pending     │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │ John wants to also   │
                    │ provide services     │
                    │ (Role Switch)        │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │ PATCH /api/Auth/prof │
                    │ role: "Provider"     │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────────┐
                    │ UPDATE User              │
                    │ Role = "Provider"        │
                    │ ServiceCategories = ...  │
                    └──────────┬───────────────┘
                               │
                    ┌──────────▼───────────┐
                    │ REGISTER SERVICES    │
                    │                      │
                    │ ProviderService(s)   │
                    │ ProviderId = john_id │
                    │ IsActive = true      │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │ NOW JOHN CAN:        │
                    │                      │
                    │ ✅ Still see his own │
                    │    requests as req.  │
                    │                      │
                    │ ✅ Manage his        │
                    │    services as prov. │
                    │                      │
                    │ ✅ Bid on other      │
                    │    requests (prov)   │
                    │                      │
                    │ ✅ Get his assigned  │
                    │    jobs (prov. view) │
                    └──────────────────────┘
```

---

## 🎮 API Endpoint Routing Based on User Role

```
                    ┌─ LOGGED IN USER ─┐
                    │                  │
            ┌───────┴────────┬─────────┘
            │                │
    ┌───────▼────────┐  ┌────▼────────────┐
    │   Requester    │  │   Provider      │
    │                │  │                 │
    │ Role Check:    │  │ Role Check:     │
    │ UserRole ==    │  │ UserRole ==     │
    │ Requester      │  │ Provider        │
    └────────┬───────┘  └────────┬────────┘
             │                   │
             ▼                   ▼
    ┌─────────────────┐ ┌──────────────────┐
    │ GET /api/       │ │ GET /api/        │
    │ ServiceRequest  │ │ ServiceRequest   │
    │ (My Requests)   │ │ (My Assignments) │
    │                 │ │                  │
    │ WHERE UserId=me │ │ WHERE Assigned   │
    │                 │ │ ProviderId=me    │
    └─────────────────┘ └──────────────────┘
             │                   │
             ▼                   ▼
    ┌─────────────────┐ ┌──────────────────┐
    │ POST /api/      │ │ GET /api/        │
    │ ServiceRequest  │ │ ProviderService  │
    │ (Create Request)│ │ (My Services)    │
    └─────────────────┘ │                  │
             │          │ POST /api/       │
             │          │ ProviderService  │
             │          │ (Add Service)    │
             │          │                  │
             │          │ GET /api/        │
             │          │ ProviderService/ │
             │          │ requests/avail   │
             │          │ (Available Jobs) │
             │          │                  │
             │          │ POST /api/       │
             │          │ ServiceRequest/  │
             │          │ {id}/bid         │
             │          │ (Submit Quote)   │
             └──────────┴──────────────────┘
```

---

## 📋 Request/Response Examples

### Example 1: Requester Creates Request

```
REQUEST:
POST /api/ServiceRequest
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "bookingType": "book_now",
  "mainCategory": "Cleaning Services",
  "subCategory": "Deep Cleaning",
  "date": "2025-11-10T00:00:00Z",
  "location": "123 Main Street, Mumbai",
  "notes": "Please bring your own equipment",
  "additionalNotes": "Available after 2 PM"
}

RESPONSE (201 Created):
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "userId": "user-guid-requester",
  "bookingType": "book_now",
  "mainCategory": "Cleaning Services",
  "subCategory": "Deep Cleaning",
  "date": "2025-11-10T00:00:00Z",
  "location": "123 Main Street, Mumbai",
  "notes": "Please bring your own equipment",
  "status": "Pending",
  "createdAt": "2025-11-01T10:00:00Z",
  "assignedProviderId": null,
  "providerRating": null
}
```

### Example 2: Provider Registers Service

```
REQUEST:
POST /api/ProviderService
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "mainCategory": "Cleaning Services",
  "subCategory": "Deep Cleaning",
  "description": "Professional deep cleaning with 5+ years experience",
  "pricePerHour": 500.00,
  "pricingType": "hourly",
  "serviceAreas": "[\"Mumbai\", \"Thane\", \"Navi Mumbai\"]"
}

RESPONSE (201 Created):
{
  "id": "660f8500-f39d-52e5-b827-557766551002",
  "providerId": "user-guid-provider",
  "mainCategory": "Cleaning Services",
  "subCategory": "Deep Cleaning",
  "description": "Professional deep cleaning with 5+ years experience",
  "pricePerHour": 500.00,
  "pricingType": "hourly",
  "serviceAreas": "[\"Mumbai\", \"Thane\", \"Navi Mumbai\"]",
  "isActive": true,
  "averageRating": 0,
  "totalReviews": 0,
  "createdAt": "2025-11-01T10:05:00Z"
}
```

### Example 3: Provider Submits Bid

```
REQUEST:
POST /api/ServiceRequest/550e8400-e29b-41d4-a716-446655440001/bid
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "quoteAmount": 4500.00,
  "quoteDescription": "Will complete in 4 hours with 2 staff members"
}

RESPONSE (201 Created):
{
  "id": "770g8600-g49e-63f6-c938-668877662003",
  "requestId": "550e8400-e29b-41d4-a716-446655440001",
  "providerId": "user-guid-provider",
  "providerName": "John's Professional Cleaning",
  "providerRating": 4.8,
  "providerReviewCount": 42,
  "quoteAmount": 4500.00,
  "quoteDescription": "Will complete in 4 hours with 2 staff members",
  "status": "Pending",
  "createdAt": "2025-11-01T10:10:00Z"
}
```

### Example 4: Requester Views My Requests

```
REQUEST:
GET /api/ServiceRequest?page=1&pageSize=10&status=Pending
Authorization: Bearer eyJhbGc...

RESPONSE (200 OK):
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "userId": "user-guid-requester",
      "bookingType": "book_now",
      "mainCategory": "Cleaning Services",
      "subCategory": "Deep Cleaning",
      "date": "2025-11-10T00:00:00Z",
      "location": "123 Main Street, Mumbai",
      "status": "Pending",
      "createdAt": "2025-11-01T10:00:00Z",
      "assignedProviderId": null,
      "bids": [
        {
          "id": "770g8600-g49e-63f6-c938-668877662003",
          "providerId": "user-guid-provider",
          "providerName": "John's Professional Cleaning",
          "quoteAmount": 4500.00,
          "providerRating": 4.8
        }
      ]
    }
  ],
  "total": 5,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### Example 5: Provider Views Available Requests

```
REQUEST:
GET /api/ProviderService/requests/available?page=1&pageSize=10
Authorization: Bearer eyJhbGc...

RESPONSE (200 OK):
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "mainCategory": "Cleaning Services",
      "subCategory": "Deep Cleaning",
      "location": "123 Main Street, Mumbai",
      "date": "2025-11-10T00:00:00Z",
      "status": "Pending",
      "createdAt": "2025-11-01T10:00:00Z"
    },
    {
      "id": "880h8700-h50f-74g7-d949-779988773004",
      "mainCategory": "Cleaning Services",
      "subCategory": "Office Cleaning",
      "location": "456 Business Park, Mumbai",
      "date": "2025-11-12T00:00:00Z",
      "status": "Pending",
      "createdAt": "2025-11-01T11:30:00Z"
    }
  ],
  "total": 8,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### Example 6: Provider Views My Assignments

```
REQUEST:
GET /api/ServiceRequest?page=1&pageSize=10
Authorization: Bearer eyJhbGc...
[User role: Provider, so gets assignments instead]

RESPONSE (200 OK):
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "userId": "user-guid-requester",
      "requesterName": "Sarah Johnson",
      "mainCategory": "Cleaning Services",
      "status": "Confirmed",
      "date": "2025-11-10T00:00:00Z",
      "location": "123 Main Street, Mumbai",
      "acceptedAt": "2025-11-01T10:15:00Z",
      "providerRating": null  // Not rated yet
    }
  ],
  "total": 3,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

## 🔐 Security Matrix

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    WHO CAN DO WHAT                                      │
├──────────────────┬──────────────┬─────────────┬──────────────┬──────────┤
│ Operation        │ Requester    │ Provider    │ Admin        │ Other    │
├──────────────────┼──────────────┼─────────────┼──────────────┼──────────┤
│ Create Request   │ ✅ Own only  │ ✅ Own only │ ✅ Any       │ ❌       │
│ View Requests    │ ✅ Own only  │ ✅ Assigned │ ✅ All       │ ❌       │
│ Edit Request     │ ✅ Own only  │ ❌          │ ✅ Any       │ ❌       │
│ Cancel Request   │ ✅ Own only  │ ❌          │ ✅ Any       │ ❌       │
│                  │              │             │              │          │
│ Register Service │ ❌           │ ✅ Own      │ ✅ Any       │ ❌       │
│ View Services    │ ❌           │ ✅ Own      │ ✅ All       │ ❌       │
│ Edit Service     │ ❌           │ ✅ Own only │ ✅ Any       │ ❌       │
│ Delete Service   │ ❌           │ ✅ Own only │ ✅ Any       │ ❌       │
│                  │              │             │              │          │
│ Submit Bid       │ ❌           │ ✅ Others   │ ✅ Any       │ ❌       │
│ View Bids        │ ✅ Own req.  │ ✅ Own bid  │ ✅ All       │ ❌       │
│ Accept Bid       │ ✅ Own only  │ ❌          │ ✅ Any       │ ❌       │
│                  │              │             │              │          │
│ Rate Provider    │ ✅ After job │ ❌          │ ✅ Any       │ ❌       │
│ View Ratings     │ ✅ All       │ ✅ Own      │ ✅ All       │ ✅ View  │
├──────────────────┼──────────────┼─────────────┼──────────────┼──────────┤
│ Update Role      │ ✅ Self only │ ✅ Self only│ ✅ Any user  │ ❌       │
└──────────────────┴──────────────┴─────────────┴──────────────┴──────────┘
```

---

## 📊 State Diagram: Service Request Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────┐
│              SERVICE REQUEST STATE TRANSITIONS                          │
└─────────────────────────────────────────────────────────────────────────┘

                    ┌──────────────┐
                    │   CREATED    │ (New request with UserId, AssignedProviderId=null)
                    └──────┬───────┘
                           │
                  ┌────────┴────────┐
                  │                 │
                  ▼                 ▼
           ┌────────────┐    ┌──────────────┐
           │  PENDING   │    │  GET_QUOTE   │
           │            │    │              │
           │ BookingType│    │ BookingType  │
           │ book_now OR│    │ get_quote    │
           │schedule_lat│    │              │
           │            │    │ Waiting for  │
           │ Waiting for│    │ provider     │
           │ provider   │    │ quotes       │
           │ acceptance │    │              │
           └─────┬──────┘    └──────┬───────┘
                 │                  │
                 │ Provider bids    │ Provider quotes
                 │                  │
                 ▼                  ▼
          ┌────────────────────────────┐
          │     BIDS RECEIVED          │
          │                            │
          │ AssignedProviderId = null  │
          │ Multiple ServiceBid records│
          │                            │
          │ Requester reviews bids     │
          └──────────┬─────────────────┘
                     │
          Requester accepts bid
                     │
                     ▼
          ┌────────────────────────────┐
          │     CONFIRMED              │
          │                            │
          │ AssignedProviderId = set   │
          │ AcceptedAt = NOW()         │
          │ Status = Confirmed         │
          │                            │
          │ Provider can start job     │
          └──────────┬─────────────────┘
                     │
         Job completion or cancellation
                     │
          ┌──────────┴──────────┐
          │                     │
          ▼                     ▼
    ┌──────────────┐    ┌──────────────┐
    │  COMPLETED   │    │  CANCELLED   │
    │              │    │              │
    │ CompletedAt  │    │ CancelledAt  │
    │ = NOW()      │    │ = NOW()      │
    │              │    │ CancellReason│
    │ Ready for    │    │              │
    │ rating       │    │ No rating    │
    └──────┬───────┘    └──────────────┘
           │
           │ Requester submits rating
           │
           ▼
    ┌──────────────┐
    │   RATED      │
    │              │
    │ ProviderRat. │
    │ ProviderReview
    │              │
    └──────────────┘
```

---

## 🎯 Implementation Roadmap

```
PHASE 1: Core Infrastructure (Week 1)
├─ Create Model classes
├─ Create database migrations
├─ Update ApplicationDbContext
└─ ✅ Database ready

PHASE 2: Provider Services (Week 2)
├─ Create ProviderService interface/impl
├─ Create ProviderServiceController
├─ Add availability management
└─ ✅ Providers can register services

PHASE 3: Bidding System (Week 3)
├─ Create ServiceBid model
├─ Create bid management endpoints
├─ Implement bid acceptance logic
└─ ✅ Bidding system operational

PHASE 4: Reviews & Ratings (Week 4)
├─ Create Review model
├─ Create review endpoints
├─ Update provider rating calculations
└─ ✅ Rating system active

PHASE 5: Dashboard & Analytics (Week 5)
├─ Create dual-role dashboard endpoint
├─ Add filtering and search
├─ Add analytics endpoints
└─ ✅ Full feature ready

PHASE 6: Testing & Optimization (Week 6)
├─ Unit tests for all services
├─ Integration tests for API flows
├─ Performance optimization
└─ ✅ Production ready
```

---

## 🚀 Deployment Considerations

```
Database:
  ✅ Run all migrations
  ✅ Create indexes for performance
  ✅ Backup before applying migrations

API:
  ✅ Deploy new controllers
  ✅ Deploy updated services
  ✅ Deploy new DTOs
  ✅ Update API documentation

Client:
  ✅ Update frontend to handle role switching
  ✅ Create provider profile setup flow
  ✅ Create bid submission UI
  ✅ Create rating UI

Testing:
  ✅ Test dual-role scenarios
  ✅ Test authorization on all endpoints
  ✅ Load test with multiple roles
  ✅ Security audit
```

This guide provides a comprehensive overview of the dual-role implementation! 🎉