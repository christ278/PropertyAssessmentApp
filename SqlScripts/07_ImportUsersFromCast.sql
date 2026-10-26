-- =========================================================================
-- SCRIPT: 07_ImportUsersFromCast.sql
-- DESCRIPTION: Imports and synchronizes all staff user accounts from 
--              legacy [DBCast].[dbo].[User] into [FairTrackDB].[dbo].[UserAccounts]
--              with proper Division, User Position, and Workflow Step Role mappings.
-- SOURCE DATABASE: DBCast
-- TARGET DATABASE: FairTrackDB
-- DATE: 2026-10-24
-- =========================================================================

USE [FairTrackDB];
GO

PRINT '=======================================================';
PRINT 'Starting Staff User Migration from DBCast to FairTrackDB';
PRINT '=======================================================';

-- 1. Ensure columns exist on target UserAccounts table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'DivisionId')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [DivisionId] INT NULL;
    PRINT 'Added DivisionId column to UserAccounts.';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'WorkflowRoleId')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [WorkflowRoleId] INT NULL;
    PRINT 'Added WorkflowRoleId column to UserAccounts.';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'WorkflowRoleName')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [WorkflowRoleName] NVARCHAR(150) NULL;
    PRINT 'Added WorkflowRoleName column to UserAccounts.';
END
GO

-- 2. Staging / CTE query with mapped positions and workflow step IDs
;WITH CastUserMapped AS (
    SELECT 
        u.uid,
        -- Deterministic GUID based on legacy CAST UID
        CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', 'CAST_USER_' + CAST(u.uid AS VARCHAR(20)))) AS [TargetId],
        
        -- Clean username (deduplicate inactive legacy admin with uid 1)
        CASE 
            WHEN LOWER(LTRIM(RTRIM(u.username))) = 'admin' AND u.uid = 1 THEN 'admin_legacy'
            ELSE LOWER(LTRIM(RTRIM(u.username)))
        END AS [CleanUsername],
        
        LTRIM(RTRIM(u.Name)) AS [FullName],
        
        -- Default ASP.NET Core Identity PasswordHasher PBKDF2 hash for password: 'P@ssword2026!'
        N'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==' AS [DefaultPasswordHash],
        N'P@ssword2026!' AS [DefaultPasswordPlain],
        
        -- Email Address
        LOWER(LTRIM(RTRIM(REPLACE(u.username, ' ', '')))) + '@gensan.gov.ph' AS [Email],
        
        -- Employee Number
        'EMP-' + RIGHT('00000' + CAST(u.uid AS VARCHAR(10)), 5) AS [EmployeeNo],
        
        -- Map User Position (Enum: 0=SystemAdmin, 1=CityAssessor, 2=AssessmentOfficer, 3=Appraiser, 4=TaxMapper, 5=AssessmentClerk)
        CASE 
            WHEN u.isAdmin = 1 OR u.Position LIKE '%Admin%' OR LOWER(u.username) = 'admin' THEN 0 -- SystemAdmin
            WHEN u.Position LIKE '%City Assessor%' OR u.Position LIKE '%Assessor Head%' OR (u.DivisionId = 1 AND u.roleid = 8) THEN 1 -- CityAssessor
            WHEN u.Position LIKE '%LAOO%' OR u.Position LIKE '%Officer%' OR u.Position LIKE '%Supervisor%' THEN 2 -- AssessmentOfficer
            WHEN u.Position LIKE '%Appraiser%' OR u.Position LIKE '%Valuation%' THEN 3 -- Appraiser
            WHEN u.Position LIKE '%Mapper%' OR u.Position LIKE '%Mapping%' OR u.Position LIKE '%Replot%' THEN 4 -- TaxMapper
            ELSE 5 -- AssessmentClerk (Clerks, Data Encoders, Operators, Aides)
        END AS [MappedRole],
        
        -- Department Name
        CASE 
            WHEN u.DivisionId = 1 THEN 'Real Property Appraisal & Assessment Division'
            WHEN u.DivisionId = 2 THEN 'GIS Spatial & Tax Mapping Section'
            WHEN u.DivisionId = 3 THEN 'Records & Certification Section'
            ELSE 'City Assessor Office - Administration'
        END AS [Department],
        
        -- Contact Number (Clean 11-digit Philippine mobile format)
        '0917' + RIGHT('0000000' + CAST(u.uid AS VARCHAR(10)), 7) AS [ContactNo],
        
        ISNULL(u.status, 1) AS [IsActive],
        ISNULL(u.CreatedDate, GETDATE()) AS [CreatedDate],
        u.DivisionId,
        
        -- Map CAST roleid to FairTrackDB TransactionStatusId
        CASE 
            WHEN u.DivisionId = 1 THEN u.roleid
            WHEN u.DivisionId = 2 THEN u.roleid + 14
            WHEN u.DivisionId = 3 THEN u.roleid + 24
            ELSE NULL
        END AS [MappedWorkflowRoleId],

        ROW_NUMBER() OVER (
            PARTITION BY LOWER(LTRIM(RTRIM(u.username))) 
            ORDER BY ISNULL(u.status, 1) DESC, u.uid DESC
        ) AS [UserRowNumber]
    FROM [DBCast].[dbo].[User] u
)
-- 3. Upsert into [FairTrackDB].[dbo].[UserAccounts]
MERGE INTO [dbo].[UserAccounts] AS Target
USING (
    SELECT 
        c.TargetId,
        c.CleanUsername,
        c.FullName,
        c.DefaultPasswordHash,
        c.DefaultPasswordPlain,
        c.Email,
        c.EmployeeNo,
        c.MappedRole,
        c.Department,
        c.ContactNo,
        c.IsActive,
        c.CreatedDate,
        c.DivisionId,
        c.MappedWorkflowRoleId,
        ts.Name AS [MappedWorkflowRoleName]
    FROM CastUserMapped c
    LEFT JOIN [dbo].[TransactionStatus] ts ON ts.TransactionStatusId = c.MappedWorkflowRoleId
    WHERE c.UserRowNumber = 1
) AS Source
ON Target.Username = Source.CleanUsername
WHEN MATCHED THEN
    UPDATE SET 
        Target.FullName = Source.FullName,
        Target.Email = Source.Email,
        Target.Role = Source.MappedRole,
        Target.Department = Source.Department,
        Target.IsActive = Source.IsActive,
        Target.DivisionId = Source.DivisionId,
        Target.WorkflowRoleId = Source.MappedWorkflowRoleId,
        Target.WorkflowRoleName = Source.MappedWorkflowRoleName
WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Id],
        [Username],
        [PasswordHash],
        [Password],
        [FullName],
        [Email],
        [EmployeeNo],
        [Role],
        [Department],
        [ContactNo],
        [IsActive],
        [CreatedDate],
        [LastLoginDate],
        [DivisionId],
        [WorkflowRoleId],
        [WorkflowRoleName]
    )
    VALUES (
        Source.TargetId,
        Source.CleanUsername,
        Source.DefaultPasswordHash,
        Source.DefaultPasswordPlain,
        Source.FullName,
        Source.Email,
        Source.EmployeeNo,
        Source.MappedRole,
        Source.Department,
        Source.ContactNo,
        Source.IsActive,
        Source.CreatedDate,
        NULL,
        Source.DivisionId,
        Source.MappedWorkflowRoleId,
        Source.MappedWorkflowRoleName
    );

PRINT 'User accounts successfully imported / synced from DBCast into FairTrackDB!';
GO

-- 4. Verification & Summary Output
SELECT 
    COUNT(*) AS [TotalUserAccounts],
    SUM(CASE WHEN DivisionId = 1 THEN 1 ELSE 0 END) AS [Division1_Appraisal],
    SUM(CASE WHEN DivisionId = 2 THEN 1 ELSE 0 END) AS [Division2_TaxMapping],
    SUM(CASE WHEN DivisionId = 3 THEN 1 ELSE 0 END) AS [Division3_Records],
    SUM(CASE WHEN DivisionId IS NULL THEN 1 ELSE 0 END) AS [SuperAdmins],
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS [ActiveUsers],
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS [InactiveUsers]
FROM [dbo].[UserAccounts];

SELECT 
    Username, 
    FullName, 
    CASE Role 
        WHEN 0 THEN 'System Administrator'
        WHEN 1 THEN 'City Assessor'
        WHEN 2 THEN 'Assessment Officer'
        WHEN 3 THEN 'Property Appraiser'
        WHEN 4 THEN 'Tax Mapper'
        WHEN 5 THEN 'Assessment Clerk'
    END AS [Position],
    CASE DivisionId
        WHEN 1 THEN 'Div 1 (Appraisal)'
        WHEN 2 THEN 'Div 2 (Tax Mapping)'
        WHEN 3 THEN 'Div 3 (Records)'
        ELSE 'Administration'
    END AS [Division],
    WorkflowRoleId AS [StepId],
    WorkflowRoleName AS [WorkflowStepRole],
    IsActive
FROM [dbo].[UserAccounts]
ORDER BY DivisionId, WorkflowRoleId, FullName;
GO
