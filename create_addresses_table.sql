-- Create addresses table
CREATE TABLE IF NOT EXISTS halulu_api.addresses (
    "Id" uuid NOT NULL,
    "ProfileId" uuid NOT NULL,
    "Label" character varying(50) NOT NULL,
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "AddressLine" text,
    "City" character varying(100),
    "State" character varying(100),
    "PostalCode" character varying(20),
    "IsPrimary" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_addresses" PRIMARY KEY ("Id")
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_addresses_ProfileId" ON halulu_api.addresses ("ProfileId");
CREATE INDEX IF NOT EXISTS "IX_addresses_ProfileId_IsPrimary" ON halulu_api.addresses ("ProfileId", "IsPrimary");