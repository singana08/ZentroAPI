-- Create schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS halulu_api;

-- Set search_path to use halulu_api schema
ALTER DATABASE halulu_db SET search_path TO halulu_api, public;