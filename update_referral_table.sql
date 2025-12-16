-- Update referral table to add missing columns
-- Run this directly in your PostgreSQL database

-- Check if referrals table exists, if not create it
CREATE TABLE IF NOT EXISTS zentro_api.referrals (
    "Id" uuid NOT NULL,
    "ReferrerId" uuid NOT NULL,
    "ReferredUserId" uuid NOT NULL,
    "ReferralCode" character varying(10) NOT NULL,
    "Status" text NOT NULL DEFAULT 'Pending',
    "BonusAmount" numeric(18,2),
    "FirstBookingId" uuid,
    "ReferredUserBookingsUsed" integer NOT NULL DEFAULT 0,
    "CompletedAt" timestamp with time zone,
    "WalletTransactionId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_referrals" PRIMARY KEY ("Id")
);

-- Add missing columns if they don't exist
DO $$ 
BEGIN
    -- Add FirstBookingId column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'zentro_api' 
                   AND table_name = 'referrals' 
                   AND column_name = 'FirstBookingId') THEN
        ALTER TABLE zentro_api.referrals ADD COLUMN "FirstBookingId" uuid;
    END IF;
    
    -- Add CompletedAt column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'zentro_api' 
                   AND table_name = 'referrals' 
                   AND column_name = 'CompletedAt') THEN
        ALTER TABLE zentro_api.referrals ADD COLUMN "CompletedAt" timestamp with time zone;
    END IF;
    
    -- Add WalletTransactionId column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'zentro_api' 
                   AND table_name = 'referrals' 
                   AND column_name = 'WalletTransactionId') THEN
        ALTER TABLE zentro_api.referrals ADD COLUMN "WalletTransactionId" uuid;
    END IF;
END $$;

-- Add indexes if they don't exist
CREATE UNIQUE INDEX IF NOT EXISTS "IX_referrals_ReferrerId_ReferredUserId" 
ON zentro_api.referrals ("ReferrerId", "ReferredUserId");

CREATE INDEX IF NOT EXISTS "IX_referrals_ReferrerId" 
ON zentro_api.referrals ("ReferrerId");

CREATE INDEX IF NOT EXISTS "IX_referrals_ReferredUserId" 
ON zentro_api.referrals ("ReferredUserId");

CREATE INDEX IF NOT EXISTS "IX_referrals_Status" 
ON zentro_api.referrals ("Status");

-- Add foreign key constraints if they don't exist
DO $$
BEGIN
    -- Add foreign key for ReferrerId
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                   WHERE constraint_name = 'FK_referrals_users_ReferrerId') THEN
        ALTER TABLE zentro_api.referrals 
        ADD CONSTRAINT "FK_referrals_users_ReferrerId" 
        FOREIGN KEY ("ReferrerId") REFERENCES zentro_api.users("Id") ON DELETE CASCADE;
    END IF;
    
    -- Add foreign key for ReferredUserId
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                   WHERE constraint_name = 'FK_referrals_users_ReferredUserId') THEN
        ALTER TABLE zentro_api.referrals 
        ADD CONSTRAINT "FK_referrals_users_ReferredUserId" 
        FOREIGN KEY ("ReferredUserId") REFERENCES zentro_api.users("Id") ON DELETE RESTRICT;
    END IF;
    
    -- Add foreign key for WalletTransactionId (if wallet_transactions table exists)
    IF EXISTS (SELECT 1 FROM information_schema.tables 
               WHERE table_schema = 'zentro_api' 
               AND table_name = 'wallet_transactions') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                       WHERE constraint_name = 'FK_referrals_wallet_transactions_WalletTransactionId') THEN
            ALTER TABLE zentro_api.referrals 
            ADD CONSTRAINT "FK_referrals_wallet_transactions_WalletTransactionId" 
            FOREIGN KEY ("WalletTransactionId") REFERENCES zentro_api.wallet_transactions("Id") ON DELETE SET NULL;
        END IF;
    END IF;
END $$;

-- Verify the table structure
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_schema = 'zentro_api' 
AND table_name = 'referrals'
ORDER BY ordinal_position;