-- Create ProviderMetrics table
CREATE TABLE IF NOT EXISTS halulu_api.provider_metrics (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ProviderId" uuid NOT NULL,
    "Date" date NOT NULL,
    "ResponseTimeMinutes" integer NOT NULL DEFAULT 0,
    "JobsAssigned" integer NOT NULL DEFAULT 0,
    "JobsCompleted" integer NOT NULL DEFAULT 0,
    "JobsCancelled" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_provider_metrics" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_provider_metrics_providers_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES halulu_api.providers ("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_provider_metrics_ProviderId" ON halulu_api.provider_metrics ("ProviderId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_provider_metrics_ProviderId_Date" ON halulu_api.provider_metrics ("ProviderId", "Date");
CREATE INDEX IF NOT EXISTS "IX_provider_metrics_Date" ON halulu_api.provider_metrics ("Date");