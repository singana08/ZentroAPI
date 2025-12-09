# Testing 3D Secure Payments Locally

## Setup ngrok

1. **Download ngrok**: https://ngrok.com/download
2. **Start your API**: `dotnet run` (runs on https://localhost:5001)
3. **Start ngrok**: `ngrok http 5001`
4. **Copy the URL**: e.g., `https://abc123.ngrok.io`

## Update Return URL

Your callback endpoint will automatically use the ngrok URL when accessed through it.

## Test Cards (Stripe Test Mode)

### Requires 3D Secure Authentication
- **Card**: `4000 0027 6000 3184`
- **Expiry**: Any future date (e.g., 12/25)
- **CVC**: Any 3 digits (e.g., 123)

### Always Succeeds (No 3D Secure)
- **Card**: `4242 4242 4242 4242`

## Testing Flow

1. Start API: `dotnet run`
2. Start ngrok: `ngrok http 5001`
3. Create payment intent from mobile app
4. Use test card `4000 0027 6000 3184`
5. Complete 3D Secure in browser
6. Stripe redirects to: `https://abc123.ngrok.io/api/payment/callback`
7. API redirects to: `zentroapp://payment/callback?payment_intent=xxx&status=succeeded`
8. Mobile app catches deep link

## Mobile App Deep Link Setup

### Android (AndroidManifest.xml)
```xml
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="zentroapp" android:host="payment" />
</intent-filter>
```

### iOS (Info.plist)
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>zentroapp</string>
        </array>
    </dict>
</array>
```

## Verify Callback

Test the callback manually:
```
https://abc123.ngrok.io/api/payment/callback?payment_intent=pi_xxx&payment_intent_client_secret=pi_xxx_secret_xxx
```

Should redirect to: `zentroapp://payment/callback?payment_intent=pi_xxx&status=succeeded`
