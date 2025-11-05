# Authentication Flow Guide

## Overview

The Halulu API uses a **two-step OTP-based authentication** flow that supports both email and phone number as authentication identifiers. The system automatically determines if a user is new or existing after OTP verification.

---

## Simplified Authentication Flow

### Step 1: Send OTP
**Endpoint:** `POST /api/auth/send-otp`

User requests an OTP to be sent to their email or phone number.

**Request:**
```json
{
  "email": "user@example.com",
  "phoneNumber": null
}
```

Or alternatively:

```json
{
  "email": null,
  "phoneNumber": "+1234567890"
}
```

**Response (Success - 200):**
```json
{
  "success": true,
  "message": "OTP sent successfully to your email",
  "email": "user@example.com",
  "expirationMinutes": 5
}
```

**Response (Error - 400):**
```json
{
  "success": false,
  "message": "Either email or phone number is required"
}
```

---

### Step 2: Verify OTP
**Endpoint:** `POST /api/auth/verify-otp`

User enters the OTP they received. The system:
1. ✅ Validates the OTP code
2. ✅ Checks if user exists in the database
3. ✅ Returns `isNewUser` flag to indicate user status
4. ✅ Auto-creates user account if new
5. ✅ Returns JWT token for authentication

**Request:**
```json
{
  "email": "user@example.com",
  "phoneNumber": null,
  "otpCode": "123456"
}
```

**Response (Success - New User - 200):**
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "isNewUser": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "phoneNumber": "unknown",
    "firstName": null,
    "lastName": null,
    "profileImageUrl": null,
    "role": 0,
    "isProfileComplete": false,
    "isActive": true,
    "createdAt": "2024-10-30T13:30:58.123Z",
    "lastLoginAt": null
  }
}
```

**Response (Success - Existing User - 200):**
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "isNewUser": false,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "phoneNumber": "+1234567890",
    "firstName": "John",
    "lastName": "Doe",
    "profileImageUrl": "https://...",
    "role": 0,
    "isProfileComplete": true,
    "isActive": true,
    "createdAt": "2024-10-20T10:15:30.123Z",
    "lastLoginAt": "2024-10-30T13:25:45.123Z"
  }
}
```

**Response (Invalid OTP - 400):**
```json
{
  "success": false,
  "message": "Invalid or expired OTP"
}
```

---

## Complete User Flows

### 🆕 New User Registration Flow

1. **Send OTP**
   ```
   POST /api/auth/send-otp
   Body: { "email": "newuser@example.com" }
   ```

2. **Receive OTP** (e.g., "123456" sent to email)

3. **Verify OTP**
   ```
   POST /api/auth/verify-otp
   Body: { 
     "email": "newuser@example.com",
     "otpCode": "123456"
   }
   
   Response: { isNewUser: true, token, user with isProfileComplete: false }
   ```

4. **Display Profile Completion Form**
   - Show form requesting: First Name, Last Name, Phone Number, Role

5. **Complete Profile**
   ```
   POST /api/auth/register
   Body: {
     "email": "newuser@example.com",
     "firstName": "John",
     "lastName": "Doe",
     "phoneNumber": "+1234567890",
     "role": 0
   }
   
   Response: { user with isProfileComplete: true }
   ```

6. **Dashboard Redirect**
   - Use received JWT token for authenticated requests
   - User is fully onboarded

---

### 👤 Existing User Login Flow

1. **Send OTP**
   ```
   POST /api/auth/send-otp
   Body: { "email": "existinguser@example.com" }
   ```

2. **Receive OTP**

3. **Verify OTP**
   ```
   POST /api/auth/verify-otp
   Body: { 
     "email": "existinguser@example.com",
     "otpCode": "123456"
   }
   
   Response: { isNewUser: false, token, complete user data }
   ```

4. **Direct Dashboard Access**
   - Use JWT token immediately
   - No profile completion needed
   - User is logged in and ready

---

## Usage Scenarios

### Scenario 1: User logs in with email
```
1. Send OTP → email: "john@example.com", phone: null
2. Receive OTP on email
3. Verify OTP → email: "john@example.com", otp: "123456"
4. Check isNewUser flag in response
   - If true: Show profile completion form
   - If false: Redirect to dashboard
```

### Scenario 2: User logs in with phone
```
1. Send OTP → email: null, phone: "+1234567890"
2. Receive OTP via SMS
3. Verify OTP → email: null, phone: "+1234567890", otp: "123456"
4. Check isNewUser flag in response
   - If true: Show profile completion form
   - If false: Redirect to dashboard
```

### Scenario 3: Prevent duplicate emails
```
Scenario: User tries to register with email already in system
1. Send OTP → Works (no validation against existing users)
2. Verify OTP → isNewUser: false (user found with that email)
3. Response contains existing user data
4. Backend won't create duplicate due to UNIQUE constraint
```

### Scenario 4: Prevent duplicate phone numbers
```
Scenario: User tries to register with phone already in system
1. Send OTP → Works
2. Verify OTP → isNewUser: false (user found with that phone)
3. Response contains existing user data
4. Backend won't create duplicate due to UNIQUE constraint
```

---

## API Endpoints Summary

| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/auth/send-otp` | POST | ❌ No | Request OTP for email or phone |
| `/api/auth/verify-otp` | POST | ❌ No | Verify OTP and get JWT token + user status |
| `/api/auth/register` | POST | ❌ No | Complete user profile after OTP verification |
| `/api/auth/me` | GET | ✅ Yes | Get current user information |
| `/api/auth/user/{email}` | GET | ✅ Yes | Get user by email |

---

## Key Design Decisions

### 1. **Two-Step Process**
- **Step 1 (Send OTP)**: Just sends OTP, no user checks
- **Step 2 (Verify OTP)**: Validates OTP, checks user status, returns `isNewUser` flag

### 2. **Flexible Identifiers**
- Support both email and phone number
- Email is primary if both provided
- At least one identifier required

### 3. **Auto-Registration**
- New users are automatically created on first OTP verification
- Profile completion is optional for first login
- `isProfileComplete: false` indicates incomplete profile

### 4. **Duplicate Prevention**
- Email column has UNIQUE constraint
- PhoneNumber column has UNIQUE constraint
- "unknown" placeholder used when identifier not provided during signup

### 5. **User Status Detection**
- Determined **after** OTP verification
- `isNewUser: true` → User needs profile completion
- `isNewUser: false` → User is ready for dashboard

---

## Error Handling

### Common Errors

**Neither email nor phone provided:**
```json
{
  "success": false,
  "message": "Either email or phone number is required"
}
```

**Invalid email format:**
```json
{
  "success": false,
  "message": "Invalid email address"
}
```

**Too many failed attempts (rate limited):**
```json
{
  "success": false,
  "message": "This identifier is temporarily locked due to too many failed attempts. Please try again later."
}
```

**Invalid or expired OTP:**
```json
{
  "success": false,
  "message": "Invalid or expired OTP"
}
```

**Invalid request body:**
```json
{
  "success": false,
  "message": "Invalid request",
  "errors": {
    "validation": ["OTP must be between 4-10 characters"]
  }
}
```

---

## Implementation Details

### OTP Generation
- Generated when `/send-otp` is called
- Valid for 5 minutes
- Rate limited to prevent abuse
- Can retry after rate limit expires

### Token Generation
- JWT token generated on successful OTP verification
- Contains user ID in claims
- Used for authenticated requests
- Include in `Authorization: Bearer <token>` header

### User Auto-Creation
- Triggered on first OTP verification
- Creates user with `isProfileComplete: false`
- Default role: `Requester`
- Unspecified identifiers set to "unknown"

### Profile Completion
- Only required for new users
- Updates first name, last name, phone, and role
- Sets `isProfileComplete: true`
- Triggers welcome email

---

## Best Practices for Frontend

1. **Always check `isNewUser` flag** after OTP verification
   - If true: Show profile completion form
   - If false: Proceed to dashboard

2. **Store JWT token securely**
   - Use secure HTTP-only cookies
   - Or local storage with security measures

3. **Use token for authenticated requests**
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

4. **Handle rate limiting gracefully**
   - Show user-friendly message about temporary lock
   - Implement retry logic with backoff

5. **Validate identifiers client-side**
   - Before sending OTP
   - Provide feedback immediately

---

## Testing Guide

### Test New User Flow
```bash
# 1. Send OTP
curl -X POST http://localhost:5000/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"email": "newuser@test.com"}'

# 2. Verify OTP (use OTP from logs)
curl -X POST http://localhost:5000/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{"email": "newuser@test.com", "otpCode": "123456"}'

# Response should have: isNewUser: true
```

### Test Existing User Flow
```bash
# Repeat send-otp and verify-otp with same email
# Second time should return: isNewUser: false
```

### Test Phone-based Login
```bash
# 1. Send OTP to phone
curl -X POST http://localhost:5000/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "+1234567890"}'

# 2. Verify OTP
curl -X POST http://localhost:5000/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "+1234567890", "otpCode": "123456"}'
```

---

## Migration Notes

If upgrading from previous authentication system:

1. Existing users remain unchanged
2. OTP records are maintained
3. New users use simplified flow
4. Both systems can coexist temporarily

---

## Support

For issues or questions about the authentication flow:
- Check logs for detailed error messages
- Verify OTP expiration times
- Test with both email and phone identifiers
- Review rate limiting settings for your environment