-- Add QuoteId column to messages table if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'zentro_api' 
        AND table_name = 'messages' 
        AND column_name = 'QuoteId'
    ) THEN
        ALTER TABLE zentro_api.messages ADD COLUMN "QuoteId" uuid NULL;
        RAISE NOTICE 'QuoteId column added to messages table';
    ELSE
        RAISE NOTICE 'QuoteId column already exists in messages table';
    END IF;
END $$;