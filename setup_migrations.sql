-- Setup migrations history table in halulu_api schema
CREATE TABLE IF NOT EXISTS halulu_api."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Set default search path for the database
ALTER DATABASE halulu_db SET search_path TO halulu_api, public;