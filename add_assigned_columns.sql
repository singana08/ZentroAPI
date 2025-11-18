-- Add assigned status columns to workflow_statuses table
ALTER TABLE halulu_api.workflow_statuses 
ADD COLUMN "IsAssigned" boolean NOT NULL DEFAULT false,
ADD COLUMN "AssignedDate" timestamp with time zone;