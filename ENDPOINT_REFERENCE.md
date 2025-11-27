# Halulu API - Complete Endpoint Reference

This document lists all API endpoints with their exact controller method names for consistency.

## Authentication Endpoints (`AuthController`)

### 1. Send OTP
- **Endpoint:** `POST /api/auth/send-otp`
- **Method Name:** `SendOtp`
- **Purpose:** Generate and send 6-digit OTP to email
- **Authentication:** None (Anonymous)

### 2. Verify OTP
- **Endpoint:** `POST /api/auth/verify-otp`
- **Method Name:** `VerifyOtp`
- **Purpose:** Verify OTP code and return JWT token
- **Authentication:** None (Anonymous)

### 3. Register/Complete Profile
- **Endpoint:** `POST /api/auth/register`
- **Method Name:** `Register`
- **Purpose:** Complete user profile after OTP verification
- **Authentication:** None (Anonymous)

### 4. Get Current User
- **Endpoint:** `GET /api/auth/me`
- **Method Name:** `Me`
- **Purpose:** Get current authenticated user details
- **Authentication:** Required (Bearer Token)

### 5. Get User by Email
- **Endpoint:** `GET /api/auth/user/{email}`
- **Method Name:** `GetUserByEmail`
- **Purpose:** Get user details by email
- **Authentication:** Required (Bearer Token)

### 6. Add Profile
- **Endpoint:** `POST /api/auth/add-profile`
- **Method Name:** `AddProfile`
- **Purpose:** Add additional profile (Provider or Requester)
- **Authentication:** Required (Bearer Token)

### 7. Switch Role
- **Endpoint:** `POST /api/auth/switch-role`
- **Method Name:** `SwitchRole`
- **Purpose:** Switch user role and get new token
- **Authentication:** Required (Bearer Token)

### 8. Switch to Role
- **Endpoint:** `POST /api/auth/switch-to-role`
- **Method Name:** `SwitchToRole`
- **Purpose:** Switch to different role and get new token
- **Authentication:** Required (Bearer Token)

### 9. Test Authentication
- **Endpoint:** `GET /api/auth/test-auth`
- **Method Name:** `TestAuth`
- **Purpose:** Test authentication endpoint
- **Authentication:** Required (Bearer Token)

### 10. Test Token
- **Endpoint:** `GET /api/auth/test-token`
- **Method Name:** `TestToken`
- **Purpose:** Test token parsing without authentication
- **Authentication:** None (Anonymous)

## Health Check Endpoints (`HealthController`)

### 1. Test API
- **Endpoint:** `GET /api/health/test`
- **Method Name:** `Test`
- **Purpose:** Test endpoint to verify API is accessible
- **Authentication:** None (Anonymous)

### 2. Ping Health Check
- **Endpoint:** `GET /api/health/ping`
- **Method Name:** `Ping`
- **Purpose:** Basic health check
- **Authentication:** None (Anonymous)

### 3. Status Health Check
- **Endpoint:** `GET /api/health/status`
- **Method Name:** `Status`
- **Purpose:** Detailed health status
- **Authentication:** None (Anonymous)

## Service Request Endpoints (`ServiceRequestController`)

### 1. Create Service Request
- **Endpoint:** `POST /api/servicerequest`
- **Method Name:** `Create`
- **Purpose:** Create a new service request
- **Authentication:** Required (Bearer Token)

### 2. Get Service Requests (Auto-detect)
- **Endpoint:** `GET /api/servicerequest`
- **Method Name:** `Get`
- **Purpose:** Get service requests (auto-detects provider vs requester)
- **Authentication:** Required (Bearer Token)

### 3. Update Service Request
- **Endpoint:** `PUT /api/servicerequest/{id}`
- **Method Name:** `Update`
- **Purpose:** Update service request details
- **Authentication:** Required (Bearer Token)

### 4. Cancel Service Request
- **Endpoint:** `DELETE /api/servicerequest/{id}/cancel`
- **Method Name:** `Cancel`
- **Purpose:** Cancel service request
- **Authentication:** Required (Bearer Token)

### 5. Get Requester Service Requests
- **Endpoint:** `GET /api/servicerequest/requester`
- **Method Name:** `Requester`
- **Purpose:** Get requester's own service requests
- **Authentication:** Required (Bearer Token)

### 6. Get Provider Service Requests
- **Endpoint:** `GET /api/servicerequest/provider`
- **Method Name:** `Provider`
- **Purpose:** Get provider's jobs (quoted/assigned requests)
- **Authentication:** Required (Bearer Token)

### 7. Get Available Requests (Provider)
- **Endpoint:** `GET /api/servicerequest/provider/available`
- **Method Name:** `Available`
- **Purpose:** Get available service requests for providers
- **Authentication:** Required (Bearer Token)

### 8. Hide Request (Provider)
- **Endpoint:** `POST /api/servicerequest/{id}/hide`
- **Method Name:** `Hide`
- **Purpose:** Hide service request from provider view
- **Authentication:** Required (Bearer Token)

### 9. Get Service Request Details
- **Endpoint:** `GET /api/servicerequest/{id}/details`
- **Method Name:** `Details`
- **Purpose:** Get detailed request with quotes and messages
- **Authentication:** Required (Bearer Token)

### 10. Update Workflow Status
- **Endpoint:** `PUT /api/servicerequest/{id}/workflow-status`
- **Method Name:** `WorkflowStatus`
- **Purpose:** Update workflow status for assigned request
- **Authentication:** Required (Bearer Token)

## Quote Endpoints (`QuoteController`)

### 1. Create Quote
- **Endpoint:** `POST /api/quote`
- **Method Name:** `CreateQuote`
- **Purpose:** Create a quote for service request
- **Authentication:** Required (Bearer Token)

### 2. Get Quotes for Request
- **Endpoint:** `GET /api/quote/request/{requestId}`
- **Method Name:** `GetQuotesForRequest`
- **Purpose:** Get all quotes for a service request
- **Authentication:** Required (Bearer Token)

### 3. Get Provider Quotes
- **Endpoint:** `GET /api/quote/provider`
- **Method Name:** `GetProviderQuotes`
- **Purpose:** Get quotes created by current provider
- **Authentication:** Required (Bearer Token)

### 4. Update Quote
- **Endpoint:** `PUT /api/quote/{id}`
- **Method Name:** `UpdateQuote`
- **Purpose:** Update quote details
- **Authentication:** Required (Bearer Token)

### 5. Accept Quote
- **Endpoint:** `POST /api/quote/{id}/accept`
- **Method Name:** `AcceptQuote`
- **Purpose:** Accept a quote (creates agreement)
- **Authentication:** Required (Bearer Token)

### 6. Reject Quote
- **Endpoint:** `POST /api/quote/{id}/reject`
- **Method Name:** `RejectQuote`
- **Purpose:** Reject a quote
- **Authentication:** Required (Bearer Token)

## Agreement Endpoints (`AgreementController`)

### 1. Get Agreements
- **Endpoint:** `GET /api/agreement`
- **Method Name:** `GetAgreements`
- **Purpose:** Get agreements for current user
- **Authentication:** Required (Bearer Token)

### 2. Get Agreement by ID
- **Endpoint:** `GET /api/agreement/{id}`
- **Method Name:** `GetAgreement`
- **Purpose:** Get specific agreement details
- **Authentication:** Required (Bearer Token)

### 3. Update Agreement Status
- **Endpoint:** `PUT /api/agreement/{id}/status`
- **Method Name:** `UpdateAgreementStatus`
- **Purpose:** Update agreement status
- **Authentication:** Required (Bearer Token)

## Message Endpoints (`MessageController`)

### 1. Send Message
- **Endpoint:** `POST /api/message`
- **Method Name:** `SendMessage`
- **Purpose:** Send message in quote conversation
- **Authentication:** Required (Bearer Token)

### 2. Get Messages
- **Endpoint:** `GET /api/message/quote/{quoteId}`
- **Method Name:** `GetMessages`
- **Purpose:** Get messages for a quote
- **Authentication:** Required (Bearer Token)

### 3. Get Chat List
- **Endpoint:** `GET /api/message/chats`
- **Method Name:** `GetChatList`
- **Purpose:** Get list of active chats
- **Authentication:** Required (Bearer Token)

## Category Endpoints (`CategoryController`)

### 1. Get Categories
- **Endpoint:** `GET /api/category`
- **Method Name:** `GetCategories`
- **Purpose:** Get all categories
- **Authentication:** None (Anonymous)

### 2. Get Category by ID
- **Endpoint:** `GET /api/category/{id}`
- **Method Name:** `GetCategory`
- **Purpose:** Get specific category with subcategories
- **Authentication:** None (Anonymous)

### 3. Import Categories
- **Endpoint:** `POST /api/category/import`
- **Method Name:** `ImportCategories`
- **Purpose:** Import categories from JSON
- **Authentication:** Required (Admin)

## User Management Endpoints (`UserController`)

### 1. Update Profile
- **Endpoint:** `PUT /api/user/profile`
- **Method Name:** `UpdateProfile`
- **Purpose:** Update user profile information
- **Authentication:** Required (Bearer Token)

### 2. Get Profile
- **Endpoint:** `GET /api/user/profile`
- **Method Name:** `GetProfile`
- **Purpose:** Get current user profile
- **Authentication:** Required (Bearer Token)

## Address Endpoints (`AddressController`)

### 1. Create Address
- **Endpoint:** `POST /api/address`
- **Method Name:** `CreateAddress`
- **Purpose:** Create new address
- **Authentication:** Required (Bearer Token)

### 2. Get User Addresses
- **Endpoint:** `GET /api/address`
- **Method Name:** `GetUserAddresses`
- **Purpose:** Get addresses for current user
- **Authentication:** Required (Bearer Token)

### 3. Update Address
- **Endpoint:** `PUT /api/address/{id}`
- **Method Name:** `UpdateAddress`
- **Purpose:** Update address details
- **Authentication:** Required (Bearer Token)

### 4. Delete Address
- **Endpoint:** `DELETE /api/address/{id}`
- **Method Name:** `DeleteAddress`
- **Purpose:** Delete address
- **Authentication:** Required (Bearer Token)

## Notification Endpoints (`NotificationController`)

### 1. Get Notifications
- **Endpoint:** `GET /api/notification`
- **Method Name:** `GetNotifications`
- **Purpose:** Get notifications for current user
- **Authentication:** Required (Bearer Token)

### 2. Mark as Read
- **Endpoint:** `PUT /api/notification/{id}/read`
- **Method Name:** `MarkAsRead`
- **Purpose:** Mark notification as read
- **Authentication:** Required (Bearer Token)

### 3. Update Push Token
- **Endpoint:** `POST /api/notification/push-token`
- **Method Name:** `UpdatePushToken`
- **Purpose:** Update push notification token
- **Authentication:** Required (Bearer Token)

## Review Endpoints (`ReviewController`)

### 1. Create Review
- **Endpoint:** `POST /api/review`
- **Method Name:** `CreateReview`
- **Purpose:** Create review for completed service
- **Authentication:** Required (Bearer Token)

### 2. Get Reviews for Provider
- **Endpoint:** `GET /api/review/provider/{providerId}`
- **Method Name:** `GetProviderReviews`
- **Purpose:** Get reviews for a provider
- **Authentication:** None (Anonymous)

### 3. Get Reviews for Agreement
- **Endpoint:** `GET /api/review/agreement/{agreementId}`
- **Method Name:** `GetAgreementReviews`
- **Purpose:** Get reviews for an agreement
- **Authentication:** Required (Bearer Token)

## Dashboard Endpoints (`DashboardController`)

### 1. Get Requester Dashboard
- **Endpoint:** `GET /api/dashboard/requester`
- **Method Name:** `GetRequesterDashboard`
- **Purpose:** Get dashboard data for requester
- **Authentication:** Required (Bearer Token)

### 2. Get Provider Dashboard
- **Endpoint:** `GET /api/dashboard/provider`
- **Method Name:** `GetProviderDashboard`
- **Purpose:** Get dashboard data for provider
- **Authentication:** Required (Bearer Token)

## Analytics Endpoints (`AnalyticsController`)

### 1. Get Provider Analytics
- **Endpoint:** `GET /api/analytics/provider`
- **Method Name:** `GetProviderAnalytics`
- **Purpose:** Get analytics data for provider
- **Authentication:** Required (Bearer Token)

### 2. Get Request Analytics
- **Endpoint:** `GET /api/analytics/requests`
- **Method Name:** `GetRequestAnalytics`
- **Purpose:** Get analytics for service requests
- **Authentication:** Required (Bearer Token)

## Earnings Endpoints (`EarningsController`)

### 1. Get Provider Earnings
- **Endpoint:** `GET /api/earnings/provider`
- **Method Name:** `GetProviderEarnings`
- **Purpose:** Get earnings data for provider
- **Authentication:** Required (Bearer Token)

### 2. Get Earnings Summary
- **Endpoint:** `GET /api/earnings/summary`
- **Method Name:** `GetEarningsSummary`
- **Purpose:** Get earnings summary
- **Authentication:** Required (Bearer Token)

---

## Summary

The ServiceRequestController now contains only the essential endpoints that are actively used. All unused and redundant endpoints have been removed for cleaner code maintenance.

## Naming Convention

**Rule:** Endpoint path segments should match controller method names exactly.

**Examples:**
- Endpoint: `/api/auth/send-otp` → Method: `SendOtp`
- Endpoint: `/api/auth/me` → Method: `Me`
- Endpoint: `/api/health/ping` → Method: `Ping`
- Endpoint: `/api/servicerequest` → Method: `Create` (POST) / `Get` (GET)

## UI vs API Endpoint Mapping

**✅ IMPLEMENTED ENDPOINTS (10 total):**
- `POST /api/servicerequest` → `Create()`
- `GET /api/servicerequest` → `Get()`
- `PUT /api/servicerequest/{id}` → `Update()`
- `DELETE /api/servicerequest/{id}/cancel` → `Cancel()`
- `GET /api/servicerequest/requester` → `Requester()`
- `GET /api/servicerequest/provider` → `Provider()`
- `GET /api/servicerequest/provider/available` → `Available()`
- `POST /api/servicerequest/{id}/hide` → `Hide()`
- `GET /api/servicerequest/{id}/details` → `Details()`
- `PUT /api/servicerequest/{id}/workflow-status` → `WorkflowStatus()`

This ensures consistency and makes it easier to locate the corresponding controller method for any endpoint.