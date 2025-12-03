# Azure Key Vault Setup for ZentroAPI

## Required Secrets in Key Vault

Add these 7 secrets to your Azure Key Vault `zentroapi-keys`:

### 1. Database Connection
```
Name: DatabaseConnectionString
Value: [Your PostgreSQL connection string]
```

### 2. JWT Security
```
Name: JwtSecretKey
Value: [Generate using: .\generate-jwt-secret.ps1]
```

**Generate Secure JWT Key:**
```powershell
# Run this PowerShell command to generate cryptographically secure key:
.\generate-jwt-secret.ps1

# Or use online tool:
# openssl rand -base64 32
```

### 3. Email Configuration
```
Name: EmailSenderEmail
Value: [Your Gmail address]

Name: EmailSenderPassword  
Value: [Your Gmail app password]
```

### 4. Azure Blob Storage
```
Name: AzureBlobStorageConnectionString
Value: [Your Azure Storage connection string]
```

### 5. Stripe Payment
```
Name: StripeSecretKey
Value: [Your Stripe secret key from dashboard]

Name: StripePublishableKey
Value: [Your Stripe publishable key from dashboard]

Name: StripeWebhookSecret
Value: [Your Stripe webhook secret]
```

## Setup Steps

1. **Create Key Vault** in Azure Portal
2. **Add all 7 secrets** with exact names above
3. **Enable Managed Identity** on App Service
4. **Grant Key Vault access** to App Service (Get, List permissions)
5. **Deploy application**

## Security Notes

- Never commit actual secret values to git
- Use Key Vault references in production
- Rotate secrets regularly
- Monitor Key Vault access logs