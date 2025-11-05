-- Mark problematic migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20251101192448_RemoveRequesterColumns', '8.0.0')
ON CONFLICT DO NOTHING;

-- Add DefaultRole column to users table
ALTER TABLE halulu_api.users ADD COLUMN IF NOT EXISTS "DefaultRole" integer NOT NULL DEFAULT 0;