-- Add Latitude and Longitude columns to ServiceRequests table
ALTER TABLE "ServiceRequests" 
ADD COLUMN "Latitude" double precision NULL,
ADD COLUMN "Longitude" double precision NULL;