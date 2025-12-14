# Notification Debug Guide

## Issue: Notifications not sent to providers on new service request

### Quick Debug Steps:

1. **Check Provider Settings**
   ```http
   GET /api/notification/debug/providers
   ```
   Verify:
   - Providers exist
   - `IsActive = true`
   - `NotificationsEnabled = true`
   - Push tokens are registered

2. **Check Logs**
   Look for these log messages:
   ```
   "Starting notification process for service request {id}"
   "Found {count} active providers with notifications enabled"
   "Completed notification process: {count} providers notified"
   ```

3. **Manual Test**
   ```http
   POST /api/notification/test/new-request
   {
     "serviceRequestId": "your-service-request-id"
   }
   ```

### Common Issues:

1. **No Active Providers**
   - Solution: Ensure providers have `IsActive = true` and `NotificationsEnabled = true`

2. **No Push Tokens**
   - Solution: Providers must register push tokens first

3. **Silent Failures**
   - Added try-catch around notification calls
   - Check application logs for errors

### Database Fixes:

```sql
-- Enable notifications for all active providers
UPDATE providers 
SET notifications_enabled = true 
WHERE is_active = true;

-- Check provider settings
SELECT id, is_active, notifications_enabled, 
       CASE WHEN push_token IS NOT NULL THEN 'Has Token' ELSE 'No Token' END as token_status
FROM providers;
```

### Code Changes Made:

1. Added debug logging to `NotifyProvidersOfNewServiceRequestAsync`
2. Added try-catch in `ServiceRequestService.CreateServiceRequestAsync`
3. Added debug endpoint `/api/notification/debug/providers`
4. Enhanced error handling and logging

### Test Flow:

1. Create service request
2. Check logs for notification attempts
3. Use debug endpoint to verify provider settings
4. Use test endpoint to manually trigger notifications