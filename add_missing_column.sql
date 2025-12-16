-- Add missing ReferredUserBookingsUsed column
ALTER TABLE zentro_api.referrals 
ADD COLUMN IF NOT EXISTS "ReferredUserBookingsUsed" integer NOT NULL DEFAULT 0;