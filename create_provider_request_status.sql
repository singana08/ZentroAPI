-- Create provider_request_status table
CREATE TABLE IF NOT EXISTS halulu_api.provider_request_status (
    "Id" uuid NOT NULL,
    "RequestId" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "Status" text NOT NULL,
    "LastUpdated" timestamp with time zone NOT NULL,
    "QuoteId" uuid NULL,
    CONSTRAINT "PK_provider_request_status" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_provider_request_status_providers_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES halulu_api.providers ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_provider_request_status_service_requests_RequestId" FOREIGN KEY ("RequestId") REFERENCES halulu_api.service_requests ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_provider_request_status_quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES halulu_api.quotes ("Id") ON DELETE SET NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_provider_request_status_ProviderId" ON halulu_api.provider_request_status ("ProviderId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_provider_request_status_ProviderId_RequestId" ON halulu_api.provider_request_status ("ProviderId", "RequestId");
CREATE INDEX IF NOT EXISTS "IX_provider_request_status_QuoteId" ON halulu_api.provider_request_status ("QuoteId");
CREATE INDEX IF NOT EXISTS "IX_provider_request_status_RequestId" ON halulu_api.provider_request_status ("RequestId");

-- Add ExpiresAt column to quotes table if it doesn't exist
ALTER TABLE halulu_api.quotes ADD COLUMN IF NOT EXISTS "ExpiresAt" timestamp with time zone NULL;