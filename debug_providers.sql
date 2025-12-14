-- Debug script to check provider notification settings

-- Check all providers
SELECT 
    id,
    user_id,
    is_active,
    notifications_enabled,
    CASE WHEN push_token IS NOT NULL THEN 'Has Token' ELSE 'No Token' END as token_status,
    created_at
FROM providers
ORDER BY created_at DESC;

-- Count providers by status
SELECT 
    'Total Providers' as category,
    COUNT(*) as count
FROM providers
UNION ALL
SELECT 
    'Active Providers' as category,
    COUNT(*) as count
FROM providers 
WHERE is_active = true
UNION ALL
SELECT 
    'Notifications Enabled' as category,
    COUNT(*) as count
FROM providers 
WHERE notifications_enabled = true
UNION ALL
SELECT 
    'Active + Notifications Enabled' as category,
    COUNT(*) as count
FROM providers 
WHERE is_active = true AND notifications_enabled = true
UNION ALL
SELECT 
    'With Push Tokens' as category,
    COUNT(*) as count
FROM providers 
WHERE push_token IS NOT NULL;

-- Check recent service requests
SELECT 
    id,
    requester_id,
    main_category,
    sub_category,
    location,
    status,
    created_at
FROM service_requests
ORDER BY created_at DESC
LIMIT 5;