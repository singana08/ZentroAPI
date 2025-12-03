# Generate Cryptographically Secure JWT Secret
# Run this script to generate a secure 256-bit (32-byte) JWT secret

# Method 1: Using .NET Crypto
Add-Type -AssemblyName System.Security
$rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()
$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$base64Secret = [Convert]::ToBase64String($bytes)

Write-Host "Cryptographically Secure JWT Secret (Base64):" -ForegroundColor Green
Write-Host $base64Secret -ForegroundColor Yellow

# Method 2: Using PowerShell SecureRandom
$secureBytes = 1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }
$hexSecret = ($secureBytes | ForEach-Object { $_.ToString("X2") }) -join ""

Write-Host "`nAlternative Hex Secret:" -ForegroundColor Green  
Write-Host $hexSecret -ForegroundColor Yellow

Write-Host "`nAdd this to Azure Key Vault:" -ForegroundColor Cyan
Write-Host "Name: JwtSecretKey" -ForegroundColor White
Write-Host "Value: $base64Secret" -ForegroundColor White

Write-Host "`nIMPORTANT: Never commit this secret to git!" -ForegroundColor Red