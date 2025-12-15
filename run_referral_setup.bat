@echo off
echo Running referral system database setup...

echo Connecting to database and executing SQL script...
echo Please run this SQL script manually in your PostgreSQL client:
echo.
echo File: add_referral_system_tables.sql
echo.
type add_referral_system_tables.sql
echo.
echo Script completed. Please execute the above SQL in your database client.
pause