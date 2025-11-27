-- Step 1: Add QuoteId column
ALTER TABLE halulu_api.agreements ADD COLUMN "QuoteId" uuid;

-- Step 2: Populate QuoteId from existing RequestId (match with first quote for each request)
UPDATE halulu_api.agreements 
SET "QuoteId" = (
    SELECT q."Id" 
    FROM halulu_api.quotes q 
    WHERE q."RequestId" = agreements."RequestId" 
    AND q."ProviderId" = agreements."ProviderId"
    LIMIT 1
);

-- Step 3: Drop old RequestId column
ALTER TABLE halulu_api.agreements DROP COLUMN "RequestId";

-- Step 4: Add foreign key constraint
ALTER TABLE halulu_api.agreements 
ADD CONSTRAINT fk_agreements_quote 
FOREIGN KEY ("QuoteId") REFERENCES halulu_api.quotes("Id") ON DELETE CASCADE;