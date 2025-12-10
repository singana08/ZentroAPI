# Mobile Payment Integration Guide

## Updated Payment Architecture

The API now supports **dual payment providers**:
- **Cashfree**: UPI, Net Banking, Wallets (Indian payments)
- **Stripe**: Credit/Debit Cards (International + Indian cards)

## New Endpoint Structure

### Get Payment Configuration
```http
GET /api/payment/config
Authorization: Bearer <token>
```

**Response:**
```json
{
  "stripe": {
    "publishableKey": "pk_test_..."
  },
  "cashfree": {
    "appId": "your_app_id",
    "environment": "sandbox"
  }
}
```

## Payment Flow by Method

### 1. UPI Payments (Cashfree)

#### Create Order
```http
POST /api/payment/cashfree/create-order
Authorization: Bearer <token>
Content-Type: application/json

{
  "amount": 50000,
  "jobId": "job_123",
  "providerId": "provider_456",
  "quote": 450.00,
  "platformFee": 50.00
}
```

**Response:**
```json
{
  "orderId": "order_abc123",
  "paymentSessionId": "session_xyz789",
  "amount": 50000,
  "currency": "INR",
  "provider": "cashfree"
}
```

#### Mobile Integration
```javascript
// React Native example
import { CFPaymentGatewayService } from 'react-native-cashfree-pg-sdk';

const paymentObject = {
  orderId: response.orderId,
  orderAmount: (response.amount / 100).toString(),
  appId: config.cashfree.appId,
  tokenData: response.paymentSessionId,
  orderCurrency: 'INR',
  customerName: 'Customer Name',
  customerEmail: 'customer@example.com',
  customerPhone: '9999999999',
  stage: 'TEST' // or 'PROD'
};

CFPaymentGatewayService.doPayment(paymentObject);
```

### 2. Card Payments (Stripe)

#### Create Payment Intent
```http
POST /api/payment/stripe/create-payment-intent
Authorization: Bearer <token>
Content-Type: application/json

{
  "amount": 50000,
  "jobId": "job_123", 
  "providerId": "provider_456",
  "quote": 450.00,
  "platformFee": 50.00
}
```

**Response:**
```json
{
  "clientSecret": "pi_xxx_secret_yyy",
  "paymentIntentId": "pi_xxx",
  "amount": 50000,
  "currency": "inr"
}
```

#### Mobile Integration
```javascript
// React Native Stripe
import { useStripe } from '@stripe/stripe-react-native';

const { confirmPayment } = useStripe();

const { error, paymentIntent } = await confirmPayment(clientSecret, {
  paymentMethodType: 'Card',
  returnURL: 'zentroapp://payment/callback'
});
```

## Payment Status Check (Both Providers)

```http
GET /api/payment/status/{paymentIntentId_or_orderId}
Authorization: Bearer <token>
```

## Mobile App Changes Required

### 1. Update Payment Method Selection UI
```javascript
const paymentMethods = [
  { id: 'upi', name: 'UPI', provider: 'cashfree', icon: 'upi-icon' },
  { id: 'netbanking', name: 'Net Banking', provider: 'cashfree', icon: 'bank-icon' },
  { id: 'wallet', name: 'Wallets', provider: 'cashfree', icon: 'wallet-icon' },
  { id: 'card', name: 'Credit/Debit Card', provider: 'stripe', icon: 'card-icon' }
];
```

### 2. Dynamic Provider Selection
```javascript
const initiatePayment = async (paymentMethod, amount, jobDetails) => {
  if (paymentMethod.provider === 'cashfree') {
    // Use Cashfree flow
    const order = await createCashfreeOrder(amount, jobDetails);
    return processCashfreePayment(order);
  } else {
    // Use Stripe flow  
    const intent = await createStripeIntent(amount, jobDetails);
    return processStripePayment(intent);
  }
};
```

### 3. Handle Deep Link Callbacks
```javascript
// App.js - Deep link handler
const handlePaymentCallback = (url) => {
  const params = parseDeepLinkParams(url);
  
  if (params.payment_intent) {
    // Stripe callback
    handleStripeCallback(params);
  } else if (params.order_id) {
    // Cashfree callback
    handleCashfreeCallback(params);
  }
};
```

### 4. SDK Dependencies

#### Add to package.json
```json
{
  "dependencies": {
    "@stripe/stripe-react-native": "^0.37.2",
    "react-native-cashfree-pg-sdk": "^2.0.8"
  }
}
```

#### iOS Setup (Info.plist)
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

#### Android Setup (AndroidManifest.xml)
```xml
<intent-filter>
  <action android:name="android.intent.action.VIEW" />
  <category android:name="android.intent.category.DEFAULT" />
  <category android:name="android.intent.category.BROWSABLE" />
  <data android:scheme="zentroapp" android:host="payment" />
</intent-filter>
```

## Testing

### Cashfree Test UPI ID
- Use any UPI ID in sandbox mode
- Payment will be simulated

### Stripe Test Cards
- **Success**: 4242 4242 4242 4242
- **3D Secure**: 4000 0027 6000 3184

## Error Handling

Both providers return consistent error format:
```json
{
  "error": "Error message",
  "code": "error_code"
}
```

## Migration Steps

1. **Update API calls** to use new endpoint structure
2. **Add payment method selection** UI
3. **Integrate both SDKs** (Cashfree + Stripe)
4. **Update deep link handling** for both providers
5. **Test with sandbox credentials**

## Benefits

- **Better UX**: Native UPI experience for Indian users
- **Lower fees**: Cashfree has lower transaction fees for Indian payments
- **Wider coverage**: Support for all major Indian payment methods
- **Fallback**: Stripe for international cards and backup