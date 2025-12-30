-- Add delivery status fields to messages table
DO $$ 
BEGIN
    -- Add IsDelivered column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'zentro_api' 
        AND table_name = 'messages' 
        AND column_name = 'IsDelivered'
    ) THEN
        ALTER TABLE zentro_api.messages ADD COLUMN "IsDelivered" boolean NOT NULL DEFAULT false;
        RAISE NOTICE 'IsDelivered column added to messages table';
    ELSE
        RAISE NOTICE 'IsDelivered column already exists in messages table';
    END IF;

    -- Add DeliveredAt column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'zentro_api' 
        AND table_name = 'messages' 
        AND column_name = 'DeliveredAt'
    ) THEN
        ALTER TABLE zentro_api.messages ADD COLUMN "DeliveredAt" timestamp with time zone NULL;
        RAISE NOTICE 'DeliveredAt column added to messages table';
    ELSE
        RAISE NOTICE 'DeliveredAt column already exists in messages table';
    END IF;
END $$;