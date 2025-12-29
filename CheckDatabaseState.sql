-- Check what migrations are recorded
SELECT * FROM [__EFMigrationsHistory] ORDER BY [MigrationId];

-- Check what tables exist in the database
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE' 
ORDER BY TABLE_NAME;

-- Check if WaitHistory table exists
SELECT OBJECT_ID('WaitHistory', 'U') as TableObjectId;
