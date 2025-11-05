-- Drop Address and PreferredCategories columns from requesters table
ALTER TABLE halulu_api.requesters DROP COLUMN IF EXISTS "Address";
ALTER TABLE halulu_api.requesters DROP COLUMN IF EXISTS "PreferredCategories";