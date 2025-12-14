-- Quick setup for push notification tables
-- Run this in your PostgreSQL database

CREATE TABLE IF NOT EXISTS zentro_api.user_push_tokens (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "PushToken" varchar(500) NOT NULL,
    "DeviceType" varchar(20) NOT NULL,
    "DeviceId" varchar(255),
    "AppVersion" varchar(50),
    "IsActive" boolean DEFAULT true,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_user_push_tokens_users" FOREIGN KEY ("UserId") REFERENCES zentro_api.users ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS zentro_api.notification_preferences (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "EnablePushNotifications" boolean DEFAULT true,
    "NewRequests" boolean DEFAULT true,
    "QuoteResponses" boolean DEFAULT true,
    "StatusUpdates" boolean DEFAULT true,
    "Messages" boolean DEFAULT true,
    "Reminders" boolean DEFAULT false,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_notification_preferences_users" FOREIGN KEY ("UserId") REFERENCES zentro_api.users ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS zentro_api.push_notification_logs (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "Title" varchar(255) NOT NULL,
    "Body" varchar(1000) NOT NULL,
    "Data" text,
    "Status" varchar(50) DEFAULT 'sent',
    "SentAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "DeliveredAt" timestamp with time zone,
    "ErrorMessage" varchar(1000),
    CONSTRAINT "FK_push_notification_logs_users" FOREIGN KEY ("UserId") REFERENCES zentro_api.users ("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_user_push_tokens_UserId" ON zentro_api.user_push_tokens ("UserId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_push_tokens_PushToken" ON zentro_api.user_push_tokens ("PushToken");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_notification_preferences_UserId" ON zentro_api.notification_preferences ("UserId");

-- Verify tables
SELECT tablename FROM pg_tables WHERE schemaname = 'zentro_api' AND tablename LIKE '%push%' OR tablename LIKE '%notification%';