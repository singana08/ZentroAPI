-- Add referral system tables and columns to existing database
-- Run this script manually if migrations fail

-- 1. Add referral columns to users table
ALTER TABLE zentro_api.users 
ADD COLUMN IF NOT EXISTS "ReferralCode" character varying(10),
ADD COLUMN IF NOT EXISTS "ReferredById" uuid;

-- Add unique index for referral code
CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_ReferralCode" 
ON zentro_api.users ("ReferralCode") 
WHERE "ReferralCode" IS NOT NULL;

-- 2. Create wallets table
CREATE TABLE IF NOT EXISTS zentro_api.wallets (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "Balance" numeric(18,2) NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_wallets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_wallets_users_UserId" FOREIGN KEY ("UserId") REFERENCES zentro_api.users ("Id") ON DELETE CASCADE
);

-- Add unique index for UserId
CREATE UNIQUE INDEX IF NOT EXISTS "IX_wallets_UserId" ON zentro_api.wallets ("UserId");

-- 3. Create wallet_transactions table
CREATE TABLE IF NOT EXISTS zentro_api.wallet_transactions (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "WalletId" uuid NOT NULL,
    "Type" character varying NOT NULL,
    "Source" character varying NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "BalanceAfter" numeric(18,2) NOT NULL,
    "Description" character varying(500),
    "ReferenceId" uuid,
    "ExpiresAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_wallet_transactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_wallet_transactions_wallets_WalletId" FOREIGN KEY ("WalletId") REFERENCES zentro_api.wallets ("Id") ON DELETE CASCADE
);

-- Add indexes for wallet_transactions
CREATE INDEX IF NOT EXISTS "IX_wallet_transactions_WalletId" ON zentro_api.wallet_transactions ("WalletId");
CREATE INDEX IF NOT EXISTS "IX_wallet_transactions_CreatedAt" ON zentro_api.wallet_transactions ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_wallet_transactions_ExpiresAt" ON zentro_api.wallet_transactions ("ExpiresAt");

-- 4. Create referrals table
CREATE TABLE IF NOT EXISTS zentro_api.referrals (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ReferrerId" uuid NOT NULL,
    "ReferredUserId" uuid NOT NULL,
    "ReferralCode" character varying(10) NOT NULL,
    "Status" character varying NOT NULL DEFAULT 'Pending',
    "BonusAmount" numeric(18,2) NOT NULL DEFAULT 50,
    "CompletedAt" timestamp with time zone,
    "WalletTransactionId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_referrals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_referrals_users_ReferrerId" FOREIGN KEY ("ReferrerId") REFERENCES zentro_api.users ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_referrals_users_ReferredUserId" FOREIGN KEY ("ReferredUserId") REFERENCES zentro_api.users ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_referrals_wallet_transactions_WalletTransactionId" FOREIGN KEY ("WalletTransactionId") REFERENCES zentro_api.wallet_transactions ("Id") ON DELETE SET NULL
);

-- Add indexes for referrals
CREATE UNIQUE INDEX IF NOT EXISTS "IX_referrals_ReferrerId_ReferredUserId" ON zentro_api.referrals ("ReferrerId", "ReferredUserId");
CREATE INDEX IF NOT EXISTS "IX_referrals_ReferrerId" ON zentro_api.referrals ("ReferrerId");
CREATE INDEX IF NOT EXISTS "IX_referrals_ReferredUserId" ON zentro_api.referrals ("ReferredUserId");
CREATE INDEX IF NOT EXISTS "IX_referrals_Status" ON zentro_api.referrals ("Status");

-- Verify tables were created
SELECT 
    schemaname,
    tablename 
FROM pg_tables 
WHERE schemaname = 'zentro_api' 
    AND tablename IN ('wallets', 'wallet_transactions', 'referrals')
ORDER BY tablename;