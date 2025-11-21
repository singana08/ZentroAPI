-- Create Reviews table
CREATE TABLE IF NOT EXISTS halulu_api.reviews (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ServiceRequestId" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "Rating" integer NOT NULL CHECK ("Rating" >= 1 AND "Rating" <= 5),
    "Comment" varchar(1000),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_reviews" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_reviews_service_requests_ServiceRequestId" FOREIGN KEY ("ServiceRequestId") REFERENCES halulu_api.service_requests ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_reviews_providers_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES halulu_api.providers ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_reviews_requesters_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES halulu_api.requesters ("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_reviews_ProviderId" ON halulu_api.reviews ("ProviderId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_reviews_ServiceRequestId" ON halulu_api.reviews ("ServiceRequestId");
CREATE INDEX IF NOT EXISTS "IX_reviews_CreatedAt" ON halulu_api.reviews ("CreatedAt");