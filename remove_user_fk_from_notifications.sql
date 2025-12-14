-- Migration script to remove foreign key references to Users table from notification-related tables
-- Run this script to update the database schema

-- Remove foreign key constraints from Notifications table if they exist
DO $$ 
BEGIN
    -- Check if foreign key constraint exists and drop it
    IF EXISTS (
        SELECT 1 
        FROM information_schema.table_constraints 
        WHERE constraint_name LIKE '%notifications%user%' 
        AND table_name = 'notifications'
        AND constraint_type = 'FOREIGN KEY'
    ) THEN
        -- Find and drop the specific foreign key constraint
        EXECUTE (
            SELECT 'ALTER TABLE notifications DROP CONSTRAINT ' || constraint_name || ';'
            FROM information_schema.table_constraints 
            WHERE constraint_name LIKE '%notifications%user%' 
            AND table_name = 'notifications'
            AND constraint_type = 'FOREIGN KEY'
            LIMIT 1
        );
        RAISE NOTICE 'Dropped foreign key constraint from notifications table';
    END IF;
END $$;

-- Remove foreign key constraints from UserPushTokens table if they exist
DO $$ 
BEGIN
    -- Check if foreign key constraint exists and drop it
    IF EXISTS (
        SELECT 1 
        FROM information_schema.table_constraints 
        WHERE constraint_name LIKE '%userpushtokens%user%' 
        AND table_name = 'userpushtokens'
        AND constraint_type = 'FOREIGN KEY'
    ) THEN
        -- Find and drop the specific foreign key constraint
        EXECUTE (
            SELECT 'ALTER TABLE userpushtokens DROP CONSTRAINT ' || constraint_name || ';'
            FROM information_schema.table_constraints 
            WHERE constraint_name LIKE '%userpushtokens%user%' 
            AND table_name = 'userpushtokens'
            AND constraint_type = 'FOREIGN KEY'
            LIMIT 1
        );
        RAISE NOTICE 'Dropped foreign key constraint from userpushtokens table';
    END IF;
END $$;

-- Remove foreign key constraints from NotificationPreferences table if they exist
DO $$ 
BEGIN
    -- Check if foreign key constraint exists and drop it
    IF EXISTS (
        SELECT 1 
        FROM information_schema.table_constraints 
        WHERE constraint_name LIKE '%notificationpreferences%user%' 
        AND table_name = 'notificationpreferences'
        AND constraint_type = 'FOREIGN KEY'
    ) THEN
        -- Find and drop the specific foreign key constraint
        EXECUTE (
            SELECT 'ALTER TABLE notificationpreferences DROP CONSTRAINT ' || constraint_name || ';'
            FROM information_schema.table_constraints 
            WHERE constraint_name LIKE '%notificationpreferences%user%' 
            AND table_name = 'notificationpreferences'
            AND constraint_type = 'FOREIGN KEY'
            LIMIT 1
        );
        RAISE NOTICE 'Dropped foreign key constraint from notificationpreferences table';
    END IF;
END $$;

-- Remove foreign key constraints from PushNotificationLogs table if they exist
DO $$ 
BEGIN
    -- Check if foreign key constraint exists and drop it
    IF EXISTS (
        SELECT 1 
        FROM information_schema.table_constraints 
        WHERE constraint_name LIKE '%pushnotificationlogs%user%' 
        AND table_name = 'pushnotificationlogs'
        AND constraint_type = 'FOREIGN KEY'
    ) THEN
        -- Find and drop the specific foreign key constraint
        EXECUTE (
            SELECT 'ALTER TABLE pushnotificationlogs DROP CONSTRAINT ' || constraint_name || ';'
            FROM information_schema.table_constraints 
            WHERE constraint_name LIKE '%pushnotificationlogs%user%' 
            AND table_name = 'pushnotificationlogs'
            AND constraint_type = 'FOREIGN KEY'
            LIMIT 1
        );
        RAISE NOTICE 'Dropped foreign key constraint from pushnotificationlogs table';
    END IF;
END $$;

-- Update any UserId columns to use ProfileId instead (if needed)
-- Note: This assumes ProfileId is the correct reference for notifications

-- For Notifications table - ensure it uses ProfileId instead of UserId
DO $$
BEGIN
    -- Check if UserId column exists and ProfileId doesn't
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'notifications' AND column_name = 'userid') 
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'notifications' AND column_name = 'profileid') THEN
        ALTER TABLE notifications RENAME COLUMN userid TO profileid;
        RAISE NOTICE 'Renamed UserId to ProfileId in notifications table';
    END IF;
END $$;

-- Verify the changes
SELECT 
    table_name,
    constraint_name,
    constraint_type
FROM information_schema.table_constraints 
WHERE table_name IN ('notifications', 'userpushtokens', 'notificationpreferences', 'pushnotificationlogs')
AND constraint_type = 'FOREIGN KEY'
ORDER BY table_name;

RAISE NOTICE 'Migration completed - removed foreign key references to Users table from notification-related tables';