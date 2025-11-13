-- Create agreements table
CREATE TABLE IF NOT EXISTS halulu_api.agreements (
    "Id" uuid NOT NULL,
    "RequestId" uuid NOT NULL,
    "RequesterId" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "RequesterAccepted" boolean NOT NULL DEFAULT false,
    "ProviderAccepted" boolean NOT NULL DEFAULT false,
    "RequesterAcceptedAt" timestamp with time zone,
    "ProviderAcceptedAt" timestamp with time zone,
    "FinalizedAt" timestamp with time zone,
    "Status" text NOT NULL DEFAULT 'Pending',
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_agreements" PRIMARY KEY ("Id")
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_agreements_ProviderId" ON halulu_api.agreements ("ProviderId");
CREATE INDEX IF NOT EXISTS "IX_agreements_RequesterId" ON halulu_api.agreements ("RequesterId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_agreements_RequestId_ProviderId" ON halulu_api.agreements ("RequestId", "ProviderId");
CREATE INDEX IF NOT EXISTS "IX_agreements_Status" ON halulu_api.agreements ("Status");