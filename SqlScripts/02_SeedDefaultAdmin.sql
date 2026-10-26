-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 02: DEFAULT ADMIN ACCOUNT SEEDING (T-SQL / SQL SERVER)
-- Target Account: Username: 'admin' | Password: 'x' | Role: 0 ('SystemAdmin')
-- =============================================================================

SET NOCOUNT ON;

-- INSERT OR UPDATE DEFAULT SYSTEM ADMINISTRATOR ACCOUNT
-- PasswordHash is verified for ASP.NET Core Identity PasswordHasher for password: 'x'
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
        'AQAAAAIAAYagAAAAEClMCzKQl1OaneUt2imzj7GxgSYrauY0GSDKqNAg8ss6MOlvyFqexJ7lRyltJUsJVw==', -- Hash for password 'x'
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
    SET [PasswordHash] = 'AQAAAAIAAYagAAAAEClMCzKQl1OaneUt2imzj7GxgSYrauY0GSDKqNAg8ss6MOlvyFqexJ7lRyltJUsJVw==',
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
