@echo off
echo Creating Push Notification Tables...
echo.

REM Get database connection details from appsettings
echo Please run this SQL script in your PostgreSQL database:
echo.
echo File: CreatePushNotificationTables.sql
echo.
echo This will create:
echo - user_push_tokens table
echo - notification_preferences table  
echo - push_notification_logs table
echo.
echo After running the SQL script, the push notification system will be ready!
echo.
pause