-- Create Push Notification Tables for ZentroAPI
-- Run this script to manually create the push notification tables

-- 1. UserPushTokens Table
CREATE TABLE IF NOT EXISTS zentro_api.user_push_tokens (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "PushToken" varchar(500) NOT NULL,
    "DeviceType" varchar(20) NOT NULL, -- 'ios' or 'android'
    "DeviceId" varchar(255),
    "AppVersion" varchar(50),
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_user_push_tokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_user_push_tokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES zentro_api.users ("Id") ON DELETE CASCADE
);

-- Create indexes for UserPushTokens
CREATE INDEX IF NOT EXISTS "IX_user_push_tokens_UserId" ON zentro_api.user_push_tokens ("UserId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_push_tokens_PushToken" ON zentro_api.user_push_tokens ("PushToken");
CREATE INDEX IF NOT EXISTS "IX_user_push_tokens_UserId_DeviceId" ON zentro_api.user_push_tokens ("UserId", "DeviceId");

-- 2. NotificationPreferences Table
CREATE TABLE IF NOT EXISTS zentro_api.notification_preferences (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "EnablePushNotifications" boolean NOT NULL DEFAULT true,
    "NewRequests" boolean NOT NULL DEFAULT true,
    "QuoteResponses" boolean NOT NULL DEFAULT true,
    "StatusUpdates" boolean NOT NULL DEFAULT true,
    "Messages" boolean NOT NULL DEFAULT true,
    "Reminders" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_notification_preferences" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_notification_preferences_users_UserId" FOREIGN KEY ("UserId") REFERENCES zentro_api.users ("Id") ON DELETE CASCADE
);

-- Create indexes for NotificationPreferences
CREATE UNIQUE INDEX IF NOT EXISTS "IX_notification_preferences_UserId" ON zentro_api.notification_preferences ("UserId");

-- 3. PushNotificationLogs Table
CREATE TABLE IF NOT EXISTS zentro_api.push_notification_logs (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "Title" varchar(255) NOT NULL,
    "Body" varchar(1000) NOT NULL,
    "Data" text, -- JSON string
    "Status" varchar(50) NOT NULL DEFAULT 'sent', -- 'sent', 'failed', 'delivered'
    "SentAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DeliveredAt" timestamp with time zone,
    "ErrorMessage" varchar(1000),
    CONSTRAINT "PK_push_notification_logs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_push_notification_logs_users_UserId" FOREIGN KEY ("UserId") REFERENCES zentro_api.users ("Id") ON DELETE CASCADE
);

-- Create indexes for PushNotificationLogs
CREATE INDEX IF NOT EXISTS "IX_push_notification_logs_UserId" ON zentro_api.push_notification_logs ("UserId");
CREATE INDEX IF NOT EXISTS "IX_push_notification_logs_SentAt" ON zentro_api.push_notification_logs ("SentAt");
CREATE INDEX IF NOT EXISTS "IX_push_notification_logs_Status" ON zentro_api.push_notification_logs ("Status");

-- Insert migration record to track this manual creation
INSERT INTO zentro_api."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251214074516_AddPushNotificationTables', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verify tables were created
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'zentro_api' 
AND tablename IN ('user_push_tokens', 'notification_preferences', 'push_notification_logs')
ORDER BY tablename;