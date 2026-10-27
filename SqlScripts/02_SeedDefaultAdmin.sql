-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 02: DEFAULT ADMIN ACCOUNT SEEDING (T-SQL / SQL SERVER)
-- Target Account: Username: 'admin' | Password: 'P@ssword2026!' | Role: 0 ('SystemAdmin')
-- =============================================================================

SET NOCOUNT ON;

-- INSERT OR UPDATE DEFAULT SYSTEM ADMINISTRATOR ACCOUNT
-- Password is 'P@ssword2026!' (ASP.NET Core Identity PasswordHasher PBKDF2 hash)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'Password')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] DROP COLUMN [Password];
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserAccounts] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[UserAccounts] (
        [Id],
        [Username],
        [PasswordHash],
        [FullName],
        [Email],
        [EmployeeNo],
        [Role],
        [Department],
        [ContactNo],
        [IsActive],
        [CreatedDate],
        [LastLoginDate]
    )
    VALUES (
        '70000000-0000-0000-0000-000000000000',
        'admin',
        'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==', -- Hash for password 'P@ssword2026!'
        'System Administrator',
        'admin@gensan.gov.ph',
        'EMP-ADMIN-001',
        0, -- 0: SystemAdmin Role
        'City Assessor Executive Office',
        '0917-000-0000',
        1, -- 1: Bit TRUE in T-SQL
        GETDATE(),
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [dbo].[UserAccounts]
    SET [PasswordHash] = 'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==',
        [FullName] = 'System Administrator',
        [Role] = 0,
        [IsActive] = 1,
        [LastLoginDate] = GETDATE()
    WHERE [Username] = 'admin';
END;

-- VERIFICATION QUERY FOR DEFAULT ADMIN
SELECT [Id], [Username], [FullName], [Email], [EmployeeNo], [Role], [IsActive] 
FROM [dbo].[UserAccounts] 
WHERE [Username] = 'admin';
