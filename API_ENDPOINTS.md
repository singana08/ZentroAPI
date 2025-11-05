# API Endpoints Reference

Complete API endpoint documentation for HaluluAPI.

## Base URL
```
Development: http://localhost:5000
Production: https://api.halulu.com
```

---

## Authentication Endpoints

### Send OTP
```http
POST /api/auth/send-otp
```

**Request:**
```json
{
  "email": "user@example.com",
  "phoneNumber": "+1234567890"
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "OTP sent successfully to your email",
  "email": "user@example.com",
  "expirationMinutes": 5
}
```

**Response (Error):**
```json
{
  "success": false,
  "message": "Email is temporarily locked due to too many failed attempts"
}
```

**Status Codes:**
- `200 OK` - OTP sent successfully
- `400 Bad Request` - Invalid email or locked account
- `500 Internal Server Error` - Server error

---

### Verify OTP
```http
POST /api/auth/verify-otp
```

**Request:**
```json
{
  "email": "user@example.com",
  "otpCode": "123456"
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "OTP verified successfully",
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
    "createdAt": "2024-01-15T10:30:00Z",
    "lastLoginAt": null
  },
  "isNewUser": true
}
```

**Response (Error):**
```json
{
  "success": false,
  "message": "Invalid or expired OTP"
}
```

**Status Codes:**
- `200 OK` - OTP verified successfully
- `400 Bad Request` - Invalid/expired OTP
- `500 Internal Server Error` - Server error

---

### Register / Complete Profile
```http
POST /api/auth/register
```

**Request:**
```json
{
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "role": 0
}
```

**Response (Success):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "phoneNumber": "+1234567890",
  "firstName": "John",
  "lastName": "Doe",
  "profileImageUrl": null,
  "role": 0,
  "isProfileComplete": true,
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastLoginAt": "2024-01-15T10:35:00Z"
}
```

**Response (Error):**
```json
{
  "success": false,
  "message": "User not found. Please verify OTP first.",
  "errors": {
    "email": ["Email is required"],
    "firstName": ["First name is required"]
  }
}
```

**Status Codes:**
- `200 OK` - Profile registered successfully
- `400 Bad Request` - Validation error
- `401 Unauthorized` - User not authenticated
- `500 Internal Server Error` - Server error

**Required Headers:**
```
Content-Type: application/json
```

**Field Validation:**
- `email`: Required, valid email format
- `firstName`: Required, 2-100 characters
- `lastName`: Required, 2-100 characters
- `phoneNumber`: Required, valid phone format
- `role`: Required, 0 (Requester) or 1 (Provider)

---

## User Endpoints

### Get Current User
```http
GET /api/auth/me
Authorization: Bearer <jwt_token>
```

**Response (Success):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "phoneNumber": "+1234567890",
  "firstName": "John",
  "lastName": "Doe",
  "profileImageUrl": null,
  "role": 0,
  "isProfileComplete": true,
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastLoginAt": "2024-01-15T10:35:00Z"
}
```

**Response (Error):**
```json
{
  "success": false,
  "message": "User ID not found in token"
}
```

**Status Codes:**
- `200 OK` - User information retrieved
- `401 Unauthorized` - Invalid or missing token
- `404 Not Found` - User not found
- `500 Internal Server Error` - Server error

**Required Headers:**
```
Authorization: Bearer <jwt_token>
```

---

### Get User by Email
```http
GET /api/auth/user/{email}
Authorization: Bearer <jwt_token>
```

**URL Parameters:**
- `email` (string, required): User email address

**Response (Success):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "phoneNumber": "+1234567890",
  "firstName": "John",
  "lastName": "Doe",
  "profileImageUrl": null,
  "role": 0,
  "isProfileComplete": true,
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastLoginAt": "2024-01-15T10:35:00Z"
}
```

**Response (Error):**
```json
{
  "success": false,
  "message": "User not found"
}
```

**Status Codes:**
- `200 OK` - User information retrieved
- `400 Bad Request` - Invalid email format
- `401 Unauthorized` - Invalid or missing token
- `404 Not Found` - User not found
- `500 Internal Server Error` - Server error

**Required Headers:**
```
Authorization: Bearer <jwt_token>
```

---

## Health Check Endpoints

### Health Ping
```http
GET /api/health/ping
```

**Response:**
```json
{
  "status": "ok",
  "timestamp": "2024-01-15T10:35:00Z"
}
```

**Status Codes:**
- `200 OK` - Service is healthy

---

### Health Status
```http
GET /api/health/status
```

**Response:**
```json
{
  "status": "healthy",
  "service": "HaluluAPI",
  "version": "1.0.0",
  "timestamp": "2024-01-15T10:35:00Z"
}
```

**Status Codes:**
- `200 OK` - Service is healthy

---

## Error Responses

All error responses follow this format:

```json
{
  "success": false,
  "message": "Error description",
  "errors": {
    "fieldName": ["Error message 1", "Error message 2"]
  }
}
```

### Common Error Codes

| Code | Message | Cause |
|------|---------|-------|
| 400 | Bad Request | Invalid input data |
| 401 | Unauthorized | Missing or invalid JWT token |
| 404 | Not Found | Resource doesn't exist |
| 422 | Unprocessable Entity | Validation errors |
| 429 | Too Many Requests | Rate limited |
| 500 | Internal Server Error | Server error |

---

## Authentication

### JWT Token Format

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

### Token Claims

```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "phone_number": "+1234567890",
  "name": "John Doe",
  "role": "Requester",
  "IsProfileComplete": "true",
  "IsActive": "true",
  "iat": 1705320600,
  "exp": 1705407000,
  "iss": "HaluluAPI",
  "aud": "HaluluMobileApp"
}
```

### Token Expiration

- Default: 24 hours (1440 minutes)
- Configurable in `appsettings.json`

---

## Rate Limiting

| Endpoint | Limit |
|----------|-------|
| `/api/auth/send-otp` | 5 requests per 10 minutes per email |
| `/api/auth/verify-otp` | 5 attempts per OTP |
| Others | 100 requests per minute |

After exceeding limits, receive:
```json
{
  "success": false,
  "message": "Too many requests. Please try again later."
}
```

---

## CORS Configuration

Allowed origins (configurable):
- `http://localhost:3000`
- `http://localhost:8081`
- `http://localhost:5173`
- Production domain

---

## Enums

### UserRole
```
0 = Requester
1 = Provider
2 = Admin
```

### OtpPurpose
```
0 = Authentication
1 = PhoneVerification
2 = PasswordReset
```

---

## Example Request/Response Flow

### Complete Authentication Flow

1. **User requests OTP**
```bash
curl -X POST http://localhost:5000/api/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com"}'
```

2. **Server sends OTP to email**
```json
{
  "success": true,
  "message": "OTP sent successfully to your email",
  "expirationMinutes": 5
}
```

3. **User verifies OTP**
```bash
curl -X POST http://localhost:5000/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","otpCode":"123456"}'
```

4. **Server returns JWT token**
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "isNewUser": true
}
```

5. **User registers profile**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"user@example.com",
    "firstName":"John",
    "lastName":"Doe",
    "phoneNumber":"+1234567890",
    "role":0
  }'
```

6. **User accesses protected endpoint**
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

---

## WebSocket Support (Future)

Real-time notifications for:
- OTP verification status
- Profile updates
- Message notifications

Coming in v2.0

---

## API Versioning

Current version: `v1`

Future versions will be available at:
- `/api/v2/auth/...`
- `/api/v3/auth/...`

---

## Support

For API issues and questions:
- 📧 Email: support@halulu.com
- 🐛 GitHub Issues: [HaluluAPI Issues](https://github.com/halulu/HaluluAPI/issues)
- 📚 Documentation: [README.md](README.md)

---

**Last Updated:** 2024-01-15
**API Version:** 1.0.0