-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 04: FILE LOCATION SETUP & SEED SCRIPT (CAST REFERENCE SYSTEM)
-- Reference Source: CAST_v1 / SqlScript / seed_filelocation.sql
-- =============================================================================

-- 1. ENSURE FILE LOCATION TABLE EXISTS
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FileLocation')
BEGIN
    CREATE TABLE [dbo].[FileLocation] (
        [FileLocationId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(150) NOT NULL,
        [Location] NVARCHAR(500) NOT NULL
    );
END
GO

-- 2. ENABLE IDENTITY INSERT TO PRESERVE CAST DEFAULT LOCATION IDS
SET IDENTITY_INSERT [dbo].[FileLocation] ON;
GO

-- ID 1: Profile (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 1)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (1, N'Profile', N'\\192.168.0.182\mapping_system\Attachments\UserProfile');
END

-- ID 2: Attachment (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 2)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (2, N'Attachment', N'\\192.168.0.182\mapping_system\Attachments\Attachment _for_Testing');
END

-- ID 3: Sketch (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 3)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (3, N'Sketch', N'\\192.168.0.182\mapping_system\Attachments\Skech');
END

-- ID 4: InspectionAttachment (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 4)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (4, N'InspectionAttachment', N'\\192.168.0.182\mapping_system\Attachments\Inspection');
END

-- ID 5: SketchLand (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 5)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (5, N'SketchLand', N'\\192.168.0.182\mapping_system\Attachments\Inspection');
END

-- ID 6: PropertyAssessment (Network Storage)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 6)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (6, N'PropertyAssessment', N'\\192.168.0.182\mapping_system\Attachments\PropertyAssessment');
END

-- ID 11: Profile (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 11)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (11, N'Profile', N'~/UploadFile/UserProfile');
END

-- ID 12: Attachment (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 12)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (12, N'Attachment', N'~/UploadFile/Attachment');
END

-- ID 13: Sketch (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 13)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (13, N'Sketch', N'~/UploadFile/Sketch');
END

-- ID 14: InspectionAttachmentNetwork (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 14)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (14, N'InspectionAttachmentNetwork', N'~/UploadFile/Inspection');
END

-- ID 15: SketchLandBase (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 15)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (15, N'SketchLandBase', N'~/UploadFile/Land');
END

-- ID 16: PropertyAssessmentBase (Local Web Base)
IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 16)
BEGIN
    INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Name], [Location]) 
    VALUES (16, N'PropertyAssessmentBase', N'~/UploadFile/PropertyAssessment');
END
GO

-- 3. DISABLE IDENTITY INSERT
SET IDENTITY_INSERT [dbo].[FileLocation] OFF;
GO

-- 4. VERIFICATION QUERY
SELECT [FileLocationId], [Name], [Location] 
FROM [dbo].[FileLocation] 
ORDER BY [FileLocationId];
GO
