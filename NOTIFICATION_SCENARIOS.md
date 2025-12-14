# Notification Scenarios Implementation

This document describes the implementation of notification scenarios for service requests in the Zentro API.

## Implemented Scenarios

### 1. New Service Request
**Trigger**: When a requester creates a new service request
**Recipients**: All active providers with notifications enabled
**Implementation**: `NotifyProvidersOfNewServiceRequestAsync()`

**Notification Details**:
- **Title**: "New Service Request"
- **Body**: "{SubCategory} service needed in {Location}"
- **Type**: "new_service_request"
- **Data**: Contains serviceRequestId, category, subCategory, location

**Code Location**: 
- Service: `ServiceRequestService.CreateServiceRequestAsync()`
- Notification: `NotificationService.NotifyProvidersOfNewServiceRequestAsync()`

### 2. Service Request Update/Edit
**Trigger**: When a requester updates an existing service request
**Recipients**: Assigned provider (if any)
**Implementation**: `NotifyProviderOfRequestUpdateAsync()`

**Notification Details**:
- **Title**: "Service Request Updated"
- **Body**: "The {SubCategory} request has been updated"
- **Type**: "request_edit"
- **Data**: Contains serviceRequestId, updateType, category, subCategory

**Code Location**:
- Service: `ServiceRequestService.UpdateServiceRequestAsync()`
- Notification: `NotificationService.NotifyProviderOfRequestUpdateAsync()`

### 3. Service Request Cancellation
**Trigger**: When a requester cancels a service request
**Recipients**: Assigned provider (if any)
**Implementation**: `NotifyProviderOfRequestUpdateAsync()`

**Notification Details**:
- **Title**: "Service Request Cancelled"
- **Body**: "The {SubCategory} request has been cancelled"
- **Type**: "request_cancel"
- **Data**: Contains serviceRequestId, updateType, category, subCategory

**Code Location**:
- Service: `ServiceRequestService.CancelServiceRequestAsync()`
- Notification: `NotificationService.NotifyProviderOfRequestUpdateAsync()`

### 4. Quote Acceptance
**Trigger**: When either requester or provider accepts a quote
**Recipients**: The other party (requester ↔ provider)
**Implementation**: `NotifyOfQuoteAcceptanceAsync()`

**Notification Details**:
- **Title**: "Quote Accepted!" or "Quote Response"
- **Body**: Varies based on who accepted
  - Requester accepts: "Your quote of ₹{Price} for {SubCategory} has been accepted!"
  - Provider accepts: "{AcceptingUserName} accepted the quote for {SubCategory}"
- **Type**: "quote_accepted"
- **Data**: Contains quoteId, serviceRequestId, price, acceptedBy

**Code Location**:
- Service: `AgreementService.RespondToAgreementAsync()`
- Notification: `NotificationService.NotifyOfQuoteAcceptanceAsync()`

## Technical Implementation

### Service Integration
The notification system is integrated into existing services:

1. **ServiceRequestService**: Calls notification methods for create, update, and cancel operations
2. **AgreementService**: Calls notification methods for quote acceptance

### Notification Types
The system supports both:
- **In-app notifications**: Stored in database for persistent viewing
- **Push notifications**: Sent via Expo push notification service

### Database Storage
Notifications are stored in the `Notifications` table with:
- ProfileId (recipient)
- Title, Body, Type
- RelatedEntityId (links to service request/quote)
- Data (JSON payload with additional context)
- Read status and timestamps

## Testing

### Manual Testing Endpoints
Development endpoints are available for testing:

```http
POST /api/notification/test/new-request
POST /api/notification/test/request-update  
POST /api/notification/test/request-cancel
POST /api/notification/test/quote-accept
```

**Request Body**:
```json
{
  "serviceRequestId": "guid",
  "quoteId": "guid", 
  "acceptingProfileId": "guid",
  "isRequester": true/false
}
```

### Integration Testing
The notification system automatically triggers during normal API operations:

1. Create a service request → All providers get notified
2. Update a service request → Assigned provider gets notified
3. Cancel a service request → Assigned provider gets notified
4. Accept a quote → Other party gets notified

## Configuration

### Push Notifications
- Requires valid Expo push tokens registered for users
- Uses existing `PushToken` fields in Provider/Requester tables
- Sends to Expo API endpoint: `https://exp.host/--/api/v2/push/send`

### Notification Preferences
Users can control notification preferences through:
- `NotificationsEnabled` flag in Provider/Requester tables
- Future: Granular preferences via NotificationPreferences table

## Error Handling

The notification system includes comprehensive error handling:
- Logs all notification attempts and failures
- Graceful degradation if push notification service fails
- Continues operation even if individual notifications fail
- No impact on core business logic if notifications fail

## Future Enhancements

1. **SMS Notifications**: Extend to support SMS via Twilio/AWS SNS
2. **Email Notifications**: Add email notifications for important events
3. **Notification Templates**: Create configurable message templates
4. **Batch Processing**: Queue notifications for better performance
5. **Analytics**: Track notification delivery and engagement rates