# Service Request Booking System - Complete Guide

## Overview

The Service Request API provides a comprehensive booking system supporting three distinct booking flows:

1. **book_now** - Immediate service booking
2. **schedule_later** - Future service scheduling
3. **get_quote** - Request pricing quote without commitment

## Quick Start

### 1. Create a Service Request

```http
POST /api/service-request
Authorization: Bearer <token>
Content-Type: application/json

{
  "bookingType": "book_now",
  "mainCategory": "Cleaning Services",
  "subCategory": "Deep Cleaning",
  "date": "2024-11-05T09:00:00Z",
  "time": "09:00",
  "location": "123 Main Street, City, State",
  "notes": "Apartment cleaning needed",
  "additionalNotes": "Please bring your own supplies"
}
```

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "660e8400-e29b-41d4-a716-446655440001",
  "bookingType": "book_now",
  "mainCategory": "Cleaning Services",
  "subCategory": "Deep Cleaning",
  "date": "2024-11-05T09:00:00Z",
  "time": "09:00",
  "location": "123 Main Street, City, State",
  "notes": "Apartment cleaning needed",
  "additionalNotes": "Please bring your own supplies",
  "status": "Pending",
  "createdAt": "2024-11-01T10:30:00Z",
  "updatedAt": null
}
```

## Booking Flows Explained

### 1. Book Now Flow

**Purpose**: Immediate service booking for same-day or specific date services

**Required Fields**:
- `bookingType`: "book_now"
- `mainCategory`: Service category
- `subCategory`: Service subcategory
- `date`: Date of service (cannot be in the past)
- `location`: Service location
- `time`: Preferred time (optional, HH:mm format)
- `notes`: Service details (optional)

**Validation Rules**:
- Date must be today or later
- Date and Location are mandatory
- Time is recommended for better scheduling

**Initial Status**: `Pending`

**Example**:
```json
{
  "bookingType": "book_now",
  "mainCategory": "Plumbing",
  "subCategory": "Pipe Repair",
  "date": "2024-11-01",
  "time": "14:00",
  "location": "456 Oak Avenue",
  "notes": "Leaking pipe under the sink"
}
```

### 2. Schedule Later Flow

**Purpose**: Schedule services for future dates with advance planning

**Required Fields**:
- `bookingType`: "schedule_later"
- `mainCategory`: Service category
- `subCategory`: Service subcategory
- `date`: Date of service (minimum: tomorrow)
- `location`: Service location
- `time`: Preferred time (optional)
- `notes`: Service details (optional)
- `additionalNotes`: Special requirements (optional, max 500 chars)

**Validation Rules**:
- Date must be at least tomorrow
- Date and Location are mandatory
- Ideal for planning future services

**Initial Status**: `Pending`

**Example**:
```json
{
  "bookingType": "schedule_later",
  "mainCategory": "Home Maintenance",
  "subCategory": "Pest Control",
  "date": "2024-11-10",
  "time": "10:00",
  "location": "789 Pine Road",
  "notes": "Monthly pest control service",
  "additionalNotes": "Home has 2 dogs, please be careful"
}
```

### 3. Get Quote Flow

**Purpose**: Request pricing and services without fixing date/time

**Required Fields**:
- `bookingType`: "get_quote"
- `mainCategory`: Service category
- `subCategory`: Service subcategory
- `location`: Service location
- `notes`: Service description (optional)
- `additionalNotes`: Special details (optional, max 500 chars)

**Validation Rules**:
- Date and Time are NOT required
- Location and category are mandatory
- Providers will provide quotes based on description

**Initial Status**: `QuoteRequested`

**Example**:
```json
{
  "bookingType": "get_quote",
  "mainCategory": "Garden Services",
  "subCategory": "Landscaping",
  "location": "101 Maple Drive",
  "notes": "Need complete yard redesign and new plantings",
  "additionalNotes": "Budget: $5000-$10000"
}
```

## API Endpoints

### 1. Create Service Request

```http
POST /api/service-request
```

**Authentication**: Required (Bearer token)

**Request Body**: `CreateServiceRequestDto`

**Response Codes**:
- `201 Created` - Request created successfully
- `400 Bad Request` - Invalid input or validation failed
- `401 Unauthorized` - Missing or invalid authentication
- `500 Internal Server Error` - Server error

---

### 2. Update Service Request

```http
PUT /api/service-request/{id}
```

**Parameters**:
- `id` (path): Service request ID (Guid)

**Authentication**: Required (Bearer token)

**Authorization**: Only the request owner can update

**Request Body**: `CreateServiceRequestDto`

**Response Codes**:
- `200 OK` - Request updated successfully
- `400 Bad Request` - Invalid input or validation failed
- `401 Unauthorized` - Missing or invalid authentication
- `404 Not Found` - Request not found
- `500 Internal Server Error` - Server error

**Example**:
```http
PUT /api/service-request/550e8400-e29b-41d4-a716-446655440000
Authorization: Bearer <token>
Content-Type: application/json

{
  "bookingType": "schedule_later",
  "mainCategory": "Cleaning Services",
  "subCategory": "Deep Cleaning",
  "date": "2024-11-06",
  "time": "10:00",
  "location": "123 Main Street, City, State",
  "notes": "Updated cleaning request",
  "additionalNotes": "Please use eco-friendly products"
}
```

---

### 3. Get Single Service Request

```http
GET /api/service-request/{id}
```

**Parameters**:
- `id` (path): Service request ID (Guid)

**Authentication**: Required (Bearer token)

**Authorization**: Only the request owner can view

**Response Codes**:
- `200 OK` - Request retrieved successfully
- `401 Unauthorized` - Missing or invalid authentication
- `404 Not Found` - Request not found
- `500 Internal Server Error` - Server error

**Example**:
```http
GET /api/service-request/550e8400-e29b-41d4-a716-446655440000
Authorization: Bearer <token>
```

---

### 4. Get User Service Requests (Paginated)

```http
GET /api/service-request/user/{userId}?page=1&pageSize=10&status=Pending&bookingType=book_now&category=Cleaning
```

**Parameters**:
- `userId` (path): User ID (Guid)
- `page` (query): Page number, default: 1
- `pageSize` (query): Records per page, default: 10, max: 100
- `status` (query, optional): Filter by status (Pending, QuoteRequested, Confirmed, Completed, Cancelled)
- `bookingType` (query, optional): Filter by booking type (book_now, schedule_later, get_quote)
- `category` (query, optional): Filter by main category (partial match)

**Authentication**: Required (Bearer token)

**Authorization**: Users can only view their own requests

**Response Codes**:
- `200 OK` - Requests retrieved successfully
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Access denied to another user's requests
- `500 Internal Server Error` - Server error

**Response**:
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "userId": "660e8400-e29b-41d4-a716-446655440001",
      "bookingType": "book_now",
      "mainCategory": "Cleaning Services",
      "subCategory": "Deep Cleaning",
      "date": "2024-11-05T09:00:00Z",
      "time": "09:00",
      "location": "123 Main Street",
      "status": "Pending",
      "createdAt": "2024-11-01T10:30:00Z",
      "updatedAt": null
    }
  ],
  "total": 15,
  "page": 1,
  "pageSize": 10,
  "totalPages": 2
}
```

**Example Queries**:

```bash
# Get all requests for user
GET /api/service-request/user/660e8400-e29b-41d4-a716-446655440001

# Get pending requests only
GET /api/service-request/user/660e8400-e29b-41d4-a716-446655440001?status=Pending

# Get book_now requests
GET /api/service-request/user/660e8400-e29b-41d4-a716-446655440001?bookingType=book_now

# Get Cleaning requests, page 2
GET /api/service-request/user/660e8400-e29b-41d4-a716-446655440001?category=Cleaning&page=2&pageSize=5

# Combined filters
GET /api/service-request/user/660e8400-e29b-41d4-a716-446655440001?status=Pending&bookingType=schedule_later&category=Plumbing
```

---

### 5. Cancel Service Request

```http
DELETE /api/service-request/{id}/cancel
```

**Parameters**:
- `id` (path): Service request ID (Guid)

**Authentication**: Required (Bearer token)

**Authorization**: Only the request owner can cancel

**Response Codes**:
- `200 OK` - Request cancelled successfully
- `400 Bad Request` - Cannot cancel (already cancelled or completed)
- `401 Unauthorized` - Missing or invalid authentication
- `404 Not Found` - Request not found
- `500 Internal Server Error` - Server error

**Response**:
```json
{
  "success": true,
  "message": "Service request cancelled successfully"
}
```

**Example**:
```http
DELETE /api/service-request/550e8400-e29b-41d4-a716-446655440000/cancel
Authorization: Bearer <token>
```

---

### 6. Get All Service Requests (Admin Only)

```http
GET /api/service-request/admin/all?page=1&pageSize=10&status=Pending
```

**Parameters**:
- `page` (query): Page number, default: 1
- `pageSize` (query): Records per page, default: 10, max: 100
- `status` (query, optional): Filter by status

**Authentication**: Required (Bearer token)

**Authorization**: Admin role required (to be implemented)

**Response Codes**:
- `200 OK` - Requests retrieved successfully
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Insufficient permissions
- `500 Internal Server Error` - Server error

---

## Request/Response DTOs

### CreateServiceRequestDto

```csharp
public class CreateServiceRequestDto
{
    public string BookingType { get; set; }          // "book_now", "schedule_later", "get_quote"
    public string MainCategory { get; set; }         // Required
    public string SubCategory { get; set; }          // Required
    public DateTime? Date { get; set; }              // Required for book_now and schedule_later
    public string? Time { get; set; }                // Optional, HH:mm format
    public string Location { get; set; }             // Required
    public string? Notes { get; set; }               // Optional
    public string? AdditionalNotes { get; set; }     // Optional, max 500 chars
}
```

### ServiceRequestResponseDto

```csharp
public class ServiceRequestResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BookingType { get; set; }
    public string MainCategory { get; set; }
    public string SubCategory { get; set; }
    public DateTime? Date { get; set; }
    public string? Time { get; set; }
    public string Location { get; set; }
    public string? Notes { get; set; }
    public string? AdditionalNotes { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### PaginatedServiceRequestsDto

```csharp
public class PaginatedServiceRequestsDto
{
    public List<ServiceRequestResponseDto> Data { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

## Status Workflow

```
book_now:           Pending → Confirmed → Completed or Cancelled
schedule_later:     Pending → Confirmed → Completed or Cancelled
get_quote:          QuoteRequested → [Provider Reviews] → Confirmed → Completed or Cancelled
```

### Status Values:
- **Pending** (0): Initial state for book_now and schedule_later
- **QuoteRequested** (1): Initial state for get_quote
- **Confirmed** (2): Provider accepted the request
- **Completed** (3): Service completed
- **Cancelled** (4): Request cancelled by user or provider

## Error Responses

### BadRequest (400)

```json
{
  "success": false,
  "message": "Main category is required",
  "errors": {}
}
```

### Unauthorized (401)

```json
{
  "success": false,
  "message": "User authentication failed"
}
```

### NotFound (404)

```json
{
  "success": false,
  "message": "Service request not found"
}
```

### InternalServerError (500)

```json
{
  "success": false,
  "message": "An error occurred while creating the service request"
}
```

## Validation Rules Summary

| Field | book_now | schedule_later | get_quote | Max Length |
|-------|----------|----------------|-----------|------------|
| MainCategory | Required | Required | Required | 100 |
| SubCategory | Required | Required | Required | 100 |
| Date | Required* | Required* | Optional | - |
| Time | Optional | Optional | Not Used | 20 |
| Location | Required | Required | Required | 500 |
| Notes | Optional | Optional | Optional | 1000 |
| AdditionalNotes | Optional | Optional | Optional | 500 |

*Date must be valid date - today or later for book_now, tomorrow or later for schedule_later

## Pagination Guide

### Request Parameters
```
page=1                  # First page
pageSize=10             # 10 items per page
```

### Response Structure
```json
{
  "data": [...],        # Array of ServiceRequestResponseDto
  "total": 45,          # Total records matching filter
  "page": 1,            # Current page
  "pageSize": 10,       # Items per page
  "totalPages": 5       # Calculated: ceil(total / pageSize)
}
```

### Examples
```
# Get second page with 20 items per page
GET /api/service-request/user/{userId}?page=2&pageSize=20

# Get all pending requests with pagination
GET /api/service-request/user/{userId}?status=Pending&page=1

# Large page size
GET /api/service-request/user/{userId}?pageSize=100  # Max 100
```

## Security Considerations

1. **Authentication**: All endpoints require valid JWT bearer token
2. **Authorization**: 
   - Users can only access their own service requests
   - Users can only update/cancel their own requests
   - Admin endpoints require additional role validation
3. **Data Validation**: All inputs are validated before processing
4. **Cascading Deletes**: Deleting a user also deletes their service requests

## Database Schema

### service_requests table

```sql
CREATE TABLE halulu_api.service_requests (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL,
    BookingType VARCHAR(50) NOT NULL,
    MainCategory VARCHAR(100) NOT NULL,
    SubCategory VARCHAR(100) NOT NULL,
    Date TIMESTAMP WITH TIME ZONE,
    Time VARCHAR(20),
    Location VARCHAR(500) NOT NULL,
    Notes VARCHAR(1000),
    AdditionalNotes VARCHAR(500),
    Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    UpdatedAt TIMESTAMP WITH TIME ZONE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IX_service_requests_UserId ON service_requests(UserId);
CREATE INDEX IX_service_requests_Status ON service_requests(Status);
CREATE INDEX IX_service_requests_BookingType ON service_requests(BookingType);
CREATE INDEX IX_service_requests_UserId_Status ON service_requests(UserId, Status);
CREATE INDEX IX_service_requests_CreatedAt ON service_requests(CreatedAt);
```

## Future Enhancements

1. **Provider Assignment**: Assign requests to specific service providers
2. **Negotiation Chat**: Link requests to provider-customer chat
3. **Rating/Reviews**: Add provider ratings for completed services
4. **Feedback**: Request-specific feedback and improvements
5. **Analytics**: Track booking patterns and trends
6. **Notifications**: Real-time updates for status changes
7. **Scheduling**: Calendar integration for providers
8. **Payments**: Integrated payment for confirmed bookings

## Testing with cURL

```bash
# Create a book_now request
curl -X POST https://api.halulu.com/api/service-request \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bookingType": "book_now",
    "mainCategory": "Cleaning",
    "subCategory": "Deep Cleaning",
    "date": "2024-11-05",
    "time": "09:00",
    "location": "123 Main St",
    "notes": "Home cleaning needed"
  }'

# Get all pending requests
curl https://api.halulu.com/api/service-request/user/{userId}?status=Pending \
  -H "Authorization: Bearer YOUR_TOKEN"

# Update a request
curl -X PUT https://api.halulu.com/api/service-request/{id} \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bookingType": "schedule_later",
    "mainCategory": "Cleaning",
    "subCategory": "Deep Cleaning",
    "date": "2024-11-06",
    "location": "456 Oak Ave"
  }'

# Cancel a request
curl -X DELETE https://api.halulu.com/api/service-request/{id}/cancel \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Support & Contact

For API questions or issues, contact: support@halulu.com