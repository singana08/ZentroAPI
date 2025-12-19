-- Add ServiceType column to master_category table
ALTER TABLE zentro_api.master_category 
ADD COLUMN IF NOT EXISTS "ServiceType" character varying(20) NOT NULL DEFAULT 'Free';