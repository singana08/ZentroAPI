-- Connect to halulu_db database and create the hidden_requests table
-- Database: halulu_db
-- Schema: halulu_api
-- Table: hidden_requests

-- Ensure schema exists
CREATE SCHEMA IF NOT EXISTS halulu_api;

-- Create the hidden_requests table
CREATE TABLE IF NOT EXISTS halulu_api.hidden_requests (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ProviderId" uuid NOT NULL,
    "ServiceRequestId" uuid NOT NULL,
    "HiddenAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_hidden_requests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_hidden_requests_providers_ProviderId" FOREIGN KEY ("ProviderId") 
        REFERENCES halulu_api.providers("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_hidden_requests_service_requests_ServiceRequestId" FOREIGN KEY ("ServiceRequestId") 
        REFERENCES halulu_api.service_requests("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_hidden_requests_ProviderId" 
    ON halulu_api.hidden_requests ("ProviderId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_hidden_requests_ProviderId_ServiceRequestId" 
    ON halulu_api.hidden_requests ("ProviderId", "ServiceRequestId");

-- Verify table creation
SELECT 'Table created successfully' as status;