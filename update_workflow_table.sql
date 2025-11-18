-- Add assigned status columns to workflow_statuses table
ALTER TABLE halulu_api.workflow_statuses 
ADD COLUMN IF NOT EXISTS "IsAssigned" boolean NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS "AssignedDate" timestamp with time zone;