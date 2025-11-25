IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE TABLE [AutomationTasks] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(256) NOT NULL,
        [Name] nvarchar(500) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [NaturalLanguagePrompt] nvarchar(max) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Plan] nvarchar(max) NOT NULL,
        [Context] nvarchar(max) NOT NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_AutomationTasks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE TABLE [ExecutionHistory] (
        [Id] uniqueidentifier NOT NULL,
        [TaskId] uniqueidentifier NOT NULL,
        [ExecutionStatus] nvarchar(50) NOT NULL,
        [StartedAt] datetimeoffset NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        [DurationMs] int NOT NULL,
        [StepResults] nvarchar(max) NOT NULL,
        [FinalOutput] nvarchar(max) NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [ScreenshotUrls] nvarchar(max) NOT NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [Metadata] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ExecutionHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExecutionHistory_AutomationTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [AutomationTasks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AutomationTasks_CreatedAt] ON [AutomationTasks] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AutomationTasks_Status] ON [AutomationTasks] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AutomationTasks_UserId] ON [AutomationTasks] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AutomationTasks_UserId_Status] ON [AutomationTasks] ([UserId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ExecutionHistory_ExecutionStatus] ON [ExecutionHistory] ([ExecutionStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ExecutionHistory_StartedAt] ON [ExecutionHistory] ([StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ExecutionHistory_TaskId] ON [ExecutionHistory] ([TaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251124142707_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251124142707_InitialCreate', N'9.0.0');
END;

COMMIT;
GO

