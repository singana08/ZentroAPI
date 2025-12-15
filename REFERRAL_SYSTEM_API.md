# Referral System API Documentation

## Overview

The Zentro API referral system allows users to refer new users and earn wallet credits. The system features:

- **Fixed ₹50 bonus** for each successful referral
- **60-day expiration** on referral credits to create urgency
- **Automatic wallet management** with transaction tracking
- **Background processing** for expired credit cleanup

## Database Schema

### New Tables

#### `wallets`
- `Id` (GUID, Primary Key)
- `UserId` (GUID, Foreign Key to users.Id)
- `Balance` (DECIMAL(18,2))
- `CreatedAt` (TIMESTAMP)
- `UpdatedAt` (TIMESTAMP)

#### `wallet_transactions`
- `Id` (GUID, Primary Key)
- `WalletId` (GUID, Foreign Key to wallets.Id)
- `Type` (STRING: Credit/Debit)
- `Source` (STRING: ReferralBonus/ServicePayment/Withdrawal/Refund/Expiry)
- `Amount` (DECIMAL(18,2))
- `BalanceAfter` (DECIMAL(18,2))
- `Description` (STRING, 500 chars)
- `ReferenceId` (GUID, Optional)
- `ExpiresAt` (TIMESTAMP, Optional)
- `CreatedAt` (TIMESTAMP)

#### `referrals`
- `Id` (GUID, Primary Key)
- `ReferrerId` (GUID, Foreign Key to users.Id)
- `ReferredUserId` (GUID, Foreign Key to users.Id)
- `ReferralCode` (STRING, 10 chars)
- `Status` (STRING: Pending/Completed/Expired)
- `BonusAmount` (DECIMAL(18,2), Default: 50)
- `CompletedAt` (TIMESTAMP, Optional)
- `WalletTransactionId` (GUID, Optional)
- `CreatedAt` (TIMESTAMP)

### Updated Tables

#### `users`
- Added `ReferralCode` (STRING, 10 chars, Unique)
- Added `ReferredById` (GUID, Optional, Foreign Key to users.Id)

## API Endpoints

### 1. Get Referral Code
```http
GET /api/referral/code
Authorization: Bearer <token>
```

**Response:**
```json
{
  "referralCode": "ABC12345",
  "totalReferrals": 5,
  "totalEarnings": 250.00
}
```

### 2. Apply Referral Code
```http
POST /api/referral/use
Authorization: Bearer <token>
Content-Type: application/json

{
  "referralCode": "ABC12345"
}
```

**Response:**
```json
{
  "message": "Referral code applied successfully"
}
```

**Error Responses:**
- `400`: "You have already used a referral code"
- `400`: "Invalid referral code"
- `400`: "You cannot use your own referral code"

### 3. Get Referral Statistics
```http
GET /api/referral/stats
Authorization: Bearer <token>
```

**Response:**
```json
{
  "referralCode": "ABC12345",
  "totalReferrals": 5,
  "pendingReferrals": 2,
  "completedReferrals": 3,
  "totalEarnings": 150.00,
  "pendingEarnings": 100.00,
  "recentReferrals": [
    {
      "id": "guid",
      "referredUserName": "John Doe",
      "referredUserEmail": "john@example.com",
      "status": "Completed",
      "bonusAmount": 50.00,
      "createdAt": "2024-01-15T10:30:00Z",
      "completedAt": "2024-01-16T14:20:00Z"
    }
  ]
}
```

### 4. Get Wallet Information
```http
GET /api/referral/wallet
Authorization: Bearer <token>
```

**Response:**
```json
{
  "id": "guid",
  "balance": 250.00,
  "recentTransactions": [
    {
      "id": "guid",
      "type": "Credit",
      "source": "ReferralBonus",
      "amount": 50.00,
      "balanceAfter": 250.00,
      "description": "Referral bonus for user-guid",
      "expiresAt": "2024-03-15T10:30:00Z",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

## Business Logic Flow

### 1. User Registration Flow
1. New user completes OTP verification
2. User completes profile registration
3. **Referral completion is automatically processed** if user was referred
4. Referrer receives ₹50 credit with 60-day expiration

### 2. Referral Code Generation
- 8-character alphanumeric code (A-Z, 0-9)
- Generated on first access to `/api/referral/code`
- Stored in `users.ReferralCode` field

### 3. Referral Application
- Can only be used once per user
- Cannot use own referral code
- Creates pending referral record
- Links user to referrer via `users.ReferredById`

### 4. Referral Completion
- Triggered when referred user completes profile registration
- Changes referral status from Pending to Completed
- Adds ₹50 credit to referrer's wallet with 60-day expiration
- Creates wallet transaction record

### 5. Credit Expiration
- Background service runs daily
- Processes all credits with `ExpiresAt <= NOW()`
- Creates debit transaction for expired amount
- Updates wallet balance

## Integration Points

### AuthService Integration
The `RegisterAsync` method in `AuthService` automatically calls:
```csharp
await _referralService.ProcessReferralCompletionAsync(existingUser.Id);
```

This ensures referral bonuses are awarded when users complete their profiles.

### Background Processing
The `ReferralBackgroundService` runs daily to:
- Process expired referral credits
- Clean up old transaction records
- Maintain wallet balance accuracy

## Security Considerations

1. **Referral Code Uniqueness**: Enforced at database level
2. **Single Use**: Users can only use one referral code ever
3. **Self-Referral Prevention**: Cannot use own referral code
4. **Transaction Integrity**: All wallet operations are atomic
5. **Audit Trail**: Complete transaction history maintained

## Configuration

### Fixed Bonus Amount
Currently set to ₹50 in the `Referral` model:
```csharp
public decimal BonusAmount { get; set; } = 50; // Fixed ₹50
```

### Expiration Period
Set to 60 days in `ReferralService`:
```csharp
DateTime.UtcNow.AddDays(60) // 60-day expiry
```

### Background Service Frequency
Runs daily as configured in `ReferralBackgroundService`:
```csharp
private readonly TimeSpan _period = TimeSpan.FromHours(24); // Run daily
```

## Error Handling

All API endpoints return consistent error responses:
- `400 Bad Request`: Invalid input or business rule violation
- `401 Unauthorized`: Missing or invalid authentication token
- `500 Internal Server Error`: Unexpected system errors

## Testing Scenarios

### Happy Path
1. User A gets referral code: `ABC12345`
2. User B uses referral code during registration
3. User B completes profile
4. User A receives ₹50 credit with 60-day expiration

### Edge Cases
1. **Duplicate referral code usage**: Returns error
2. **Self-referral attempt**: Returns error
3. **Invalid referral code**: Returns error
4. **Credit expiration**: Automatically processed by background service

## Future Enhancements

1. **Progressive Bonuses**: Increase bonus for multiple referrals
2. **Referral Tiers**: Different bonus amounts based on user type
3. **Seasonal Promotions**: Temporary bonus increases
4. **Referral Analytics**: Detailed reporting and insights
5. **Social Sharing**: Integration with social media platforms

## Migration

To apply the referral system to your database:

```bash
dotnet ef database update --context ApplicationDbContext
```

This will create all necessary tables and relationships for the referral system.