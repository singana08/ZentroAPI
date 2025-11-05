# Enhanced User Registration & Profile Completion Guide

## Overview

This guide describes the new enhanced registration system with:
1. **UniqueUserId** - 8-digit alphanumeric code for each user
2. **Role-based Registration** - Different required fields for Requesters vs Providers
3. **Auto-generated Profile IDs** - Unique identifier generated on first OTP verification

---

## Database Schema Updates

### New Columns Added to `Users` Table

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `UniqueUserId` | varchar(8) | NO | Auto-generated 8-character alphanumeric identifier |
| `FullName` | text | YES | User's full name |
| `Address` | text | YES | User's address |
| `ServiceCategories` | text (JSON) | YES | Categories of services (for providers) |
| `ExperienceYears` | int | YES | Years of experience (for providers) |
| `Bio` | text | YES | User biography (for providers) |
| `ServiceAreas` | text (JSON) | YES | Geographic areas of service (for providers) |
| `PricingModel` | text | YES | Pricing model (e.g., hourly, fixed) (for providers) |

---

## API Workflow

### Step 1: Send OTP
```
POST /api/auth/send-otp
```

**Request:**
```json
{
  "email": "user@example.com",
  "phoneNumber": "",
  "purpose": 0
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

---

### Step 2: Verify OTP (Auto-creates User)

```
POST /api/auth/verify-otp
```

**Request:**
```json
{
  "email": "user@example.com",
  "phoneNumber": "",
  "otpCode": "123456",
  "purpose": 0
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
    "uniqueUserId": "ABC12XYZ",
    "email": "user@example.com",
    "phoneNumber": "unknown",
    "isProfileComplete": false,
    "role": 0,
    "isActive": true,
    "createdAt": "2024-10-31T10:30:00Z"
  },
  "isNewUser": true
}
```

---

### Step 3: Complete Profile (Register)

```
POST /api/auth/register
```

#### For Requesters

**Request:**
```json
{
  "fullName": "John Doe",
  "address": "123 Main Street, New York, NY 10001",
  "role": 0,
  "email": "user@example.com",
  "phoneNumber": "+1-555-0123"
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "uniqueUserId": "ABC12XYZ",
  "fullName": "John Doe",
  "address": "123 Main Street, New York, NY 10001",
  "email": "user@example.com",
  "phoneNumber": "+1-555-0123",
  "role": 0,
  "isProfileComplete": true,
  "isActive": true,
  "createdAt": "2024-10-31T10:30:00Z",
  "lastLoginAt": "2024-10-31T10:35:00Z"
}
```

---

#### For Providers

**Request:**
```json
{
  "fullName": "Jane Smith",
  "address": "456 Service Avenue, Los Angeles, CA 90001",
  "role": 1,
  "email": "provider@example.com",
  "phoneNumber": "+1-555-0456",
  "serviceCategories": ["Plumbing", "Electrical", "HVAC"],
  "experienceYears": 8,
  "bio": "Professional service provider with 8 years of experience in residential and commercial services.",
  "serviceAreas": ["Downtown LA", "Santa Monica", "West Hollywood"],
  "pricingModel": "Hourly"
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "uniqueUserId": "XYZ78DEF",
  "fullName": "Jane Smith",
  "address": "456 Service Avenue, Los Angeles, CA 90001",
  "email": "provider@example.com",
  "phoneNumber": "+1-555-0456",
  "role": 1,
  "serviceCategories": ["Plumbing", "Electrical", "HVAC"],
  "experienceYears": 8,
  "bio": "Professional service provider with 8 years of experience...",
  "serviceAreas": ["Downtown LA", "Santa Monica", "West Hollywood"],
  "pricingModel": "Hourly",
  "isProfileComplete": true,
  "isActive": true,
  "createdAt": "2024-10-31T10:30:00Z",
  "lastLoginAt": "2024-10-31T10:35:00Z"
}
```

---

## Validation Rules

### For All Users (Requester or Provider)
- ✅ **FullName** - Required, 2-200 characters
- ✅ **Address** - Required, 5-500 characters
- ✅ **Role** - Required (0 = Requester, 1 = Provider)
- ✅ **Email** - Optional (already verified via OTP)
- ✅ **PhoneNumber** - Optional (already verified via OTP)

### Additional For Providers Only
- ✅ **ServiceCategories** - Required, non-empty array
- ✅ **ExperienceYears** - Required, >= 0
- ✅ **Bio** - Optional, max 1000 characters
- ✅ **ServiceAreas** - Required, non-empty array
- ✅ **PricingModel** - Required, max 50 characters

---

## Error Responses

### Missing Required Fields (Requester)
```json
{
  "success": false,
  "message": "Full name and address are required"
}
```

### Missing Provider Fields
```json
{
  "success": false,
  "message": "Service categories are required for providers"
}
```

### User Not Found (OTP Not Verified)
```json
{
  "success": false,
  "message": "User not found. Please verify OTP first."
}
```

### Profile Already Complete
```json
{
  "success": false,
  "message": "User profile is already complete"
}
```

---

## Key Features

### 1. UniqueUserId Generation
- **Format**: 8 characters (A-Z, 0-9)
- **Generated**: Automatically on first OTP verification
- **Uniqueness**: Cryptographically random, extremely low collision chance
- **Use Case**: Can be used throughout the application as a user identifier instead of email/phone

### 2. Two-Step Authentication
- **Step 1**: Send OTP (email or phone)
- **Step 2**: Verify OTP + Auto-create User account

### 3. Three-Step Profile Completion
1. Send OTP
2. Verify OTP (auto-creates account)
3. Complete Profile (adds details, sets IsProfileComplete=true)

### 4. Role-Based Fields
- **Requesters**: Basic info (full name, address)
- **Providers**: Extended info (service details, experience, pricing)

### 5. JSON Storage for Arrays
- ServiceCategories and ServiceAreas are stored as JSON
- Automatically serialized/deserialized on save/retrieve

---

## Migration Information

### Applied Migration
- **Name**: `AddUniqueUserIdAndProviderFields`
- **Timestamp**: 20251031053019
- **Changes**:
  - Added 8 new columns to Users table
  - UniqueUserId with maxLength 8
  - All provider fields nullable (optional)

### Rollback
If needed:
```bash
dotnet ef migrations remove
```

---

## Usage Examples

### cURL Examples

**1. Send OTP**
```bash
curl -X POST http://localhost:5000/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "phoneNumber": "",
    "purpose": 0
  }'
```

**2. Verify OTP**
```bash
curl -X POST http://localhost:5000/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "phoneNumber": "",
    "otpCode": "123456",
    "purpose": 0
  }'
```

**3. Register as Requester**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "address": "123 Main Street, New York, NY 10001",
    "role": 0
  }'
```

**4. Register as Provider**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Jane Smith",
    "address": "456 Service Avenue, Los Angeles, CA 90001",
    "role": 1,
    "serviceCategories": ["Plumbing", "Electrical"],
    "experienceYears": 8,
    "bio": "Expert service provider",
    "serviceAreas": ["Downtown LA", "Santa Monica"],
    "pricingModel": "Hourly"
  }'
```

---

## Testing Checklist

- [ ] Send OTP with email
- [ ] Verify OTP returns isNewUser=true
- [ ] Check UniqueUserId is generated (8 chars)
- [ ] Register as Requester with minimal fields
- [ ] Verify IsProfileComplete=true after registration
- [ ] Try to register twice → should fail
- [ ] Register as Provider with all fields
- [ ] Verify ServiceCategories array serialization
- [ ] Test with missing provider fields → should fail
- [ ] Check JWT token includes user info
- [ ] GET /api/auth/me returns full user with UniqueUserId

---

## File Changes Summary

### New Files
- `Utilities/UniqueIdGenerator.cs` - 8-char ID generator

### Modified Files
- `Models/User.cs` - Added UniqueUserId and provider fields
- `DTOs/AuthRequests.cs` - Updated RegisterRequest DTO
- `DTOs/AuthResponses.cs` - Updated UserDto DTO
- `Services/AuthService.cs` - Enhanced registration logic
- `Controllers/AuthController.cs` - Updated logging

### Migrations
- `Migrations/20251031053019_AddUniqueUserIdAndProviderFields.cs`

---

## Next Steps

1. **Test the API** using the provided curl examples
2. **Verify database** - Check the Users table structure
3. **Update frontend** to use new registration workflow
4. **Add user profile retrieval endpoint** (optional)
5. **Implement provider filtering** (optional)

---

## Support

For issues or questions:
1. Check validation error messages in response
2. Verify user has completed OTP verification first
3. Ensure role (0 or 1) is correct for requested operation
4. Check database migration applied successfully
