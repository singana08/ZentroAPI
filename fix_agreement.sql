-- Update agreements table to use QuoteId instead of RequestId
ALTER TABLE halulu_api.agreements 
ADD COLUMN "QuoteId" uuid;

-- You can populate QuoteId from existing RequestId if needed:
-- UPDATE halulu_api.agreements 
-- SET "QuoteId" = (SELECT q."Id" FROM halulu_api.quotes q WHERE q."RequestId" = agreements."RequestId" LIMIT 1);

-- Drop the old RequestId column after data migration
-- ALTER TABLE halulu_api.agreements DROP COLUMN "RequestId";