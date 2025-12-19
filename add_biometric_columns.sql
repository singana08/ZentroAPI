-- Add biometric authentication columns to users table
ALTER TABLE zentro_api.users 
ADD COLUMN IF NOT EXISTS "BiometricPin" character varying(64),
ADD COLUMN IF NOT EXISTS "BiometricPinExpiresAt" timestamp with time zone,
ADD COLUMN IF NOT EXISTS "BiometricEnabled" boolean NOT NULL DEFAULT false;

-- Add index for biometric PIN lookups
CREATE INDEX IF NOT EXISTS "IX_users_BiometricPin" 
ON zentro_api.users ("BiometricPin") 
WHERE "BiometricPin" IS NOT NULL;