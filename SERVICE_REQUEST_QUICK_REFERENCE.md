# Service Request API - Quick Reference

## Endpoints Overview

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| `POST` | `/api/service-request` | Create new request | Required |
| `PUT` | `/api/service-request/{id}` | Update request | Required |
| `GET` | `/api/service-request/{id}` | Get single request | Required |
| `GET` | `/api/service-request/user/{userId}` | Get user's requests (paginated) | Required |
| `DELETE` | `/api/service-request/{id}/cancel` | Cancel request | Required |
| `GET` | `/api/service-request/admin/all` | Get all requests (admin) | Required |

## Booking Types

### Book Now
```json
{
  "bookingType": "book_now",
  "mainCategory": "Cleaning",
  "subCategory": "Deep Cleaning",
  "date": "2024-11-01",
  "time": "09:00",
  "location": "123 Main St",
  "notes": "Home cleaning"
}
```
- **Date**: Today or later ✓
- **Status**: Pending
- **Use Case**: Same-day or specific date booking

### Schedule Later
```json
{
  "bookingType": "schedule_later",
  "mainCategory": "Plumbing",
  "subCategory": "Pipe Repair",
  "date": "2024-11-10",
  "location": "456 Oak Ave",
  "additionalNotes": "Call before arrival"
}
```
- **Date**: Tomorrow or later ✓
- **Status**: Pending
- **Use Case**: Future planning

### Get Quote
```json
{
  "bookingType": "get_quote",
  "mainCategory": "Landscaping",
  "subCategory": "Garden Design",
  "location": "789 Pine Rd",
  "notes": "Full yard redesign needed"
}
```
- **Date**: Not required ✓
- **Status**: QuoteRequested
- **Use Case**: Price inquiry

## Request Template

```json
{
  "bookingType": "book_now|schedule_later|get_quote",
  "mainCategory": "string (required)",
  "subCategory": "string (required)",
  "date": "2024-11-01T00:00:00Z (nullable)",
  "time": "HH:mm (optional)",
  "location": "string (required, max 500)",
  "notes": "string (optional, max 1000)",
  "additionalNotes": "string (optional, max 500)"
}
```

## Response Template

```json
{
  "id": "uuid",
  "userId": "uuid",
  "bookingType": "book_now|schedule_later|get_quote",
  "mainCategory": "string",
  "subCategory": "string",
  "date": "datetime or null",
  "time": "string or null",
  "location": "string",
  "notes": "string or null",
  "additionalNotes": "string or null",
  "status": "Pending|QuoteRequested|Confirmed|Completed|Cancelled",
  "createdAt": "datetime",
  "updatedAt": "datetime or null"
}
```

## Common Queries

### Get All Requests
```
GET /api/service-request/user/{userId}
```

### Get Pending Requests Only
```
GET /api/service-request/user/{userId}?status=Pending
```

### Get by Booking Type
```
GET /api/service-request/user/{userId}?bookingType=book_now
GET /api/service-request/user/{userId}?bookingType=schedule_later
GET /api/service-request/user/{userId}?bookingType=get_quote
```

### Search by Category
```
GET /api/service-request/user/{userId}?category=Cleaning
```

### Paginate Results
```
GET /api/service-request/user/{userId}?page=1&pageSize=10
GET /api/service-request/user/{userId}?page=2&pageSize=20
```

### Combined Filters
```
GET /api/service-request/user/{userId}?status=Pending&bookingType=book_now&category=Cleaning&page=1&pageSize=10
```

## Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success (GET, PUT, DELETE) |
| 201 | Created (POST) |
| 400 | Bad Request (invalid input) |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (access denied) |
| 404 | Not Found (request doesn't exist) |
| 500 | Server Error |

## Validation Quick Check

| Field | book_now | schedule_later | get_quote |
|-------|:--------:|:---------------:|:---------:|
| mainCategory | ✓ | ✓ | ✓ |
| subCategory | ✓ | ✓ | ✓ |
| date | ✓ | ✓ | ✗ |
| time | ◇ | ◇ | ✗ |
| location | ✓ | ✓ | ✓ |
| notes | ◇ | ◇ | ◇ |
| additionalNotes | ◇ | ◇ | ◇ |

Legend: ✓ Required | ◇ Optional | ✗ Not Used

## Statuses

- **Pending** - Initial state for book_now and schedule_later
- **QuoteRequested** - Initial state for get_quote (waiting for provider quotes)
- **Confirmed** - Provider accepted the request
- **Completed** - Service completed
- **Cancelled** - Request cancelled

## cURL Examples

### Create Book Now Request
```bash
curl -X POST https://api.halulu.com/api/service-request \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bookingType": "book_now",
    "mainCategory": "Cleaning",
    "subCategory": "Deep Cleaning",
    "date": "2024-11-01",
    "location": "123 Main St"
  }'
```

### Get User Requests
```bash
curl https://api.halulu.com/api/service-request/user/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer TOKEN"
```

### Update Request
```bash
curl -X PUT https://api.halulu.com/api/service-request/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bookingType": "schedule_later",
    "mainCategory": "Cleaning",
    "subCategory": "Deep Cleaning",
    "date": "2024-11-02",
    "location": "456 Oak Ave"
  }'
```

### Cancel Request
```bash
curl -X DELETE https://api.halulu.com/api/service-request/550e8400-e29b-41d4-a716-446655440000/cancel \
  -H "Authorization: Bearer TOKEN"
```

## Error Examples

### Invalid Booking Type
```json
{
  "success": false,
  "message": "Invalid booking type. Must be: book_now, schedule_later, or get_quote"
}
```

### Missing Required Field
```json
{
  "success": false,
  "message": "Main category is required"
}
```

### Date in Past
```json
{
  "success": false,
  "message": "Date cannot be in the past"
}
```

### Schedule Tomorrow
```json
{
  "success": false,
  "message": "Schedule date must be at least tomorrow"
}
```

### Unauthorized
```json
{
  "success": false,
  "message": "User authentication failed"
}
```

## Tips & Best Practices

1. **Always include Bearer token** - All endpoints require authentication
2. **Validate dates** - Use ISO 8601 format: `2024-11-01T09:00:00Z`
3. **Use pagination** - Don't fetch all requests at once
4. **Check status** - Monitor request status for updates
5. **Handle errors gracefully** - Parse error messages for user feedback
6. **Respect max lengths** - Location (500), Notes (1000), Additional Notes (500)
7. **Use filters** - Filter by status, booking type, or category to reduce data
8. **Implement retry logic** - For temporary server errors (5xx)

## Database

**Table**: `halulu_api.service_requests`

**Primary Key**: `Id` (UUID)

**Foreign Key**: `UserId` → `Users.Id` (CASCADE DELETE)

**Indexes**:
- UserId (for user queries)
- Status (for status filtering)
- BookingType (for type filtering)
- UserId + Status (for common combined queries)
- CreatedAt (for sorting by date)