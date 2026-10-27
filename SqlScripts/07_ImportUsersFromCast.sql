-- =========================================================================
-- SCRIPT: 07_ImportUsersFromCast.sql
-- DESCRIPTION: Standalone seed script for all staff user accounts exported
--              from legacy CAST into [dbo].[UserAccounts] with Division,
--              User Position, and Workflow Step Role mappings.
--              Self-contained INSERT/MERGE script (does NOT require DBCast).
-- DEFAULT PASSWORD FOR STAFF ACCOUNTS: P@ssword2026!
-- =========================================================================
SET NOCOUNT ON;
GO

PRINT '=======================================================';
PRINT 'Seeding Staff User Accounts into [dbo].[UserAccounts]';
PRINT '=======================================================';

-- 1. Ensure columns exist on target UserAccounts table and drop legacy Password column if present
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'Password')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] DROP COLUMN [Password];
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'DivisionId')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [DivisionId] INT NULL;
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'WorkflowRoleId')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [WorkflowRoleId] INT NULL;
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserAccounts' AND COLUMN_NAME = 'WorkflowRoleName')
BEGIN
    ALTER TABLE [dbo].[UserAccounts] ADD [WorkflowRoleName] NVARCHAR(150) NULL;
END
GO

-- Default ASP.NET Core Identity PasswordHasher PBKDF2 hash for password: 'P@ssword2026!'
DECLARE @StaffPasswordHash NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEJCWgjd6BWE3zenbZXsPT0DWIi8VACrUhCfR9GgZcewr+N8yLSdGyvJBr9pS8Zr31w==';

-- 2. Insert / Update Staff User Accounts
MERGE INTO [dbo].[UserAccounts] AS Target
USING (
    VALUES
         ('2ec18a78-087e-e7fc-1828-1f8f38ccffcf', N'leonilie', @StaffPasswordHash, N'Leonilie T. Escara', N'leonilie@gensan.gov.ph', N'EMP-13037', 4, N'Records & Certification Section', N'09170013037', 1, 3, 29, N'Approval (Records)')
        ,('1f78ef08-01ac-1c10-bde1-314cc56747ad', N'ariane', @StaffPasswordHash, N'Ariane Jane V. Fernandez', N'ariane@gensan.gov.ph', N'EMP-13038', 5, N'Records & Certification Section', N'09170013038', 1, 3, 28, N'Verification (Records)')
        ,('9b90d7a3-f796-c8c0-ae00-751a67c2e100', N'ruben', @StaffPasswordHash, N'Ruben M. Frugalidad', N'ruben@gensan.gov.ph', N'EMP-13039', 2, N'Records & Certification Section', N'09170013039', 1, 3, 28, N'Verification (Records)')
        ,('652e039f-33ae-bde3-3a3a-358cb7c2b10e', N'aillen', @StaffPasswordHash, N'Aillen R. Defante', N'aillen@gensan.gov.ph', N'EMP-13040', 5, N'Records & Certification Section', N'09170013040', 1, 3, 25, N'Receiving (Records)')
        ,('4a4ad407-c69b-2007-59a4-3bf587763113', N'wilma', @StaffPasswordHash, N'Wilma D. Paciente', N'wilma@gensan.gov.ph', N'EMP-13041', 5, N'Records & Certification Section', N'09170013041', 1, 3, 25, N'Receiving (Records)')
        ,('94f2bd77-b5ea-4e38-f295-7e28411ec938', N'angelica', @StaffPasswordHash, N'Angelica O. Ranit', N'angelica@gensan.gov.ph', N'EMP-13042', 5, N'Records & Certification Section', N'09170013042', 1, 3, 27, N'Preparation (Records)')
        ,('bd58563b-7a47-6996-1698-867236e646f1', N'john', @StaffPasswordHash, N'John Vincent Bueza', N'john@gensan.gov.ph', N'EMP-13045', 4, N'Records & Certification Section', N'09170013045', 1, 3, 26, N'Replotting (Records)')
        ,('380740a8-11e7-d622-6ea0-9aea5b66d062', N'kaila', @StaffPasswordHash, N'Kaila Fate S. Fresco', N'kaila@gensan.gov.ph', N'EMP-13047', 5, N'Real Property Appraisal & Assessment Division', N'09170013047', 1, 1, 3, N'Inspection')
        ,('78f9a635-d63b-a8a8-6643-bdfcea957562', N'genia', @StaffPasswordHash, N'Genia G. Redo-do', N'genia@gensan.gov.ph', N'EMP-13049', 5, N'Real Property Appraisal & Assessment Division', N'09170013049', 1, 1, 1, N'Receiving')
        ,('f57a2001-8cb3-9688-5e4e-fdbdacebfef4', N'alexis', @StaffPasswordHash, N'Alexis Jane M. Barcelona', N'alexis@gensan.gov.ph', N'EMP-13050', 2, N'Real Property Appraisal & Assessment Division', N'09170013050', 1, 1, 1, N'Receiving')
        ,('89f8fc32-a4c5-1660-f9fa-5920175a3590', N'norlyn', @StaffPasswordHash, N'Norlyn Mae P. Buenavides', N'norlyn@gensan.gov.ph', N'EMP-13051', 5, N'Real Property Appraisal & Assessment Division', N'09170013051', 1, 1, 1, N'Receiving')
        ,('69f23cec-bccd-4c05-a595-1c77492d5e01', N'krish', @StaffPasswordHash, N'Krish Y. Estellina', N'krish@gensan.gov.ph', N'EMP-13052', 5, N'Real Property Appraisal & Assessment Division', N'09170013052', 1, 1, 3, N'Inspection')
        ,('c9bfb6c5-3f1e-bb80-3d9e-de090b2d816e', N'jim', @StaffPasswordHash, N'Jim P. Fernandez jr.', N'jim@gensan.gov.ph', N'EMP-13053', 5, N'Real Property Appraisal & Assessment Division', N'09170013053', 1, 1, 3, N'Inspection')
        ,('3460a53f-0139-423b-5429-4a0c56f3e4db', N'reymart', @StaffPasswordHash, N'Reymart A. Librado', N'reymart@gensan.gov.ph', N'EMP-13054', 4, N'Real Property Appraisal & Assessment Division', N'09170013054', 1, 1, 3, N'Inspection')
        ,('db88bf48-7a75-2fb6-da6a-bfbbd83ac75c', N'renan', @StaffPasswordHash, N'Renan V. Osoteo', N'renan@gensan.gov.ph', N'EMP-13055', 5, N'Real Property Appraisal & Assessment Division', N'09170013055', 1, 1, 3, N'Inspection')
        ,('88a13fe0-20a7-9a82-5493-d473d1c0b502', N'suzette', @StaffPasswordHash, N'Suzette M. Gascon', N'suzette@gensan.gov.ph', N'EMP-13056', 5, N'Real Property Appraisal & Assessment Division', N'09170013056', 1, 1, 3, N'Inspection')
        ,('bff20fe1-aca6-8c65-b569-f78531187775', N'ar jell', @StaffPasswordHash, N'Ar Jell John L. Estember', N'arjell@gensan.gov.ph', N'EMP-13060', 0, N'Real Property Appraisal & Assessment Division', N'09170013060', 1, 1, 3, N'Inspection')
        ,('7c3951c5-86b3-9c54-202e-37a5d71a8c19', N'fernan', @StaffPasswordHash, N'Fernan B. Fariaria', N'fernan@gensan.gov.ph', N'EMP-13061', 5, N'Real Property Appraisal & Assessment Division', N'09170013061', 1, 1, 3, N'Inspection')
        ,('f37e2bc1-c92b-40b2-1724-c5d71901fe16', N'john rey', @StaffPasswordHash, N'John Rey G. Peusca', N'johnrey@gensan.gov.ph', N'EMP-13063', 5, N'Real Property Appraisal & Assessment Division', N'09170013063', 1, 1, 3, N'Inspection')
        ,('05ca446a-89b8-09f5-a0bb-efd160fa2798', N'jonathan', @StaffPasswordHash, N'Jonathan John C. Parreño', N'jonathan@gensan.gov.ph', N'EMP-13064', 4, N'Real Property Appraisal & Assessment Division', N'09170013064', 1, 1, 3, N'Inspection')
        ,('33fce2b5-cad9-e02c-a3f1-36902a338a5e', N'gian', @StaffPasswordHash, N'Gian Nicolai B. Fabrigas', N'gian@gensan.gov.ph', N'EMP-13065', 4, N'Real Property Appraisal & Assessment Division', N'09170013065', 1, 1, 3, N'Inspection')
        ,('48f4445b-66ec-5dbb-7163-132ae74f3aef', N'pauline', @StaffPasswordHash, N'Pauline Anne Grace R. Cruz', N'pauline@gensan.gov.ph', N'EMP-13066', 0, N'Real Property Appraisal & Assessment Division', N'09170013066', 1, 1, 4, N'Check and Review')
        ,('c13e8ffe-5860-a4dd-30e1-2b58a4979135', N'mark john', @StaffPasswordHash, N'Mark John I. Bagaforo', N'markjohn@gensan.gov.ph', N'EMP-13067', 2, N'Real Property Appraisal & Assessment Division', N'09170013067', 1, 1, 10, N'Releasing')
        ,('5fb6edef-7b5f-c12f-6e36-43e908f060ed', N'jekiriz', @StaffPasswordHash, N'Jekiriz B. Cerna', N'jekiriz@gensan.gov.ph', N'EMP-13068', 2, N'Real Property Appraisal & Assessment Division', N'09170013068', 1, 1, 4, N'Check and Review')
        ,('7e55e3ce-85c4-47b4-2761-f9abb6291e61', N'marlon', @StaffPasswordHash, N'Marlon A. Cornejo', N'marlon@gensan.gov.ph', N'EMP-13069', 2, N'Real Property Appraisal & Assessment Division', N'09170013069', 1, 1, 5, N'Repinning')
        ,('6e8965f1-190e-fc38-7e35-02ad98a1d5ba', N'ramz', @StaffPasswordHash, N'Ramizel L. Perania', N'ramz@gensan.gov.ph', N'EMP-13070', 5, N'Real Property Appraisal & Assessment Division', N'09170013070', 1, 1, 6, N'Recommending Approval')
        ,('f87fb95d-b528-0a6f-22ea-d039eb6979a6', N'rhea', @StaffPasswordHash, N'Maria Rhea Connie R. Apostol', N'rhea@gensan.gov.ph', N'EMP-13071', 0, N'Real Property Appraisal & Assessment Division', N'09170013071', 1, 1, 7, N'Valuation & Examination')
        ,('92fd4825-b1ef-baf0-9b19-47c7b3686431', N'xyrene', @StaffPasswordHash, N'Xyrene O. Caseres', N'xyrene@gensan.gov.ph', N'EMP-13072', 4, N'GIS Spatial & Tax Mapping Section', N'09170013072', 1, 2, 15, N'Receiving (GIS)')
        ,('d80f4100-5bec-a17c-dc69-a91dd6ec1bc0', N'kim', @StaffPasswordHash, N'Kim Dived E. Jabagat', N'kim@gensan.gov.ph', N'EMP-13073', 4, N'GIS Spatial & Tax Mapping Section', N'09170013073', 1, 2, 16, N'Replotting (GIS)')
        ,('952261d0-8508-744d-cc00-470a2e56a80f', N'care', @StaffPasswordHash, N'Care May B. Divinagracia', N'care@gensan.gov.ph', N'EMP-13074', 5, N'GIS Spatial & Tax Mapping Section', N'09170013074', 1, 2, 17, N'Preparation (GIS)')
        ,('846dfac1-09bb-1d32-fb0d-6e589967e339', N'mhilla', @StaffPasswordHash, N'Mhilla A. Beltran', N'mhilla@gensan.gov.ph', N'EMP-13075', 5, N'GIS Spatial & Tax Mapping Section', N'09170013075', 1, 2, 16, N'Replotting (GIS)')
        ,('a76c77fb-dd2d-4c0c-a7a1-733b920c5d66', N'marlene', @StaffPasswordHash, N'Marlene Rendaje', N'marlene@gensan.gov.ph', N'EMP-13076', 4, N'GIS Spatial & Tax Mapping Section', N'09170013076', 1, 2, 16, N'Replotting (GIS)')
        ,('ee3e8aed-764d-04a7-83a4-9e1617d9301a', N'charisse', @StaffPasswordHash, N'Charisse B. Pagay', N'charisse@gensan.gov.ph', N'EMP-13077', 4, N'GIS Spatial & Tax Mapping Section', N'09170013077', 1, 2, 18, N'Approval (GIS)')
        ,('0855a872-44b2-24d5-ccf8-558b5026f3dd', N'xian', @StaffPasswordHash, N'Christian Jeed Bade', N'xian@gensan.gov.ph', N'EMP-13078', 4, N'GIS Spatial & Tax Mapping Section', N'09170013078', 1, 2, 16, N'Replotting (GIS)')
        ,('89200c75-90d2-e962-862e-40d8b9816029', N'jobert', @StaffPasswordHash, N'Jobert Dellava', N'jobert@gensan.gov.ph', N'EMP-13079', 0, N'GIS Spatial & Tax Mapping Section', N'09170013079', 1, 2, 15, N'Receiving (GIS)')
        ,('3f615196-8130-338e-c63a-8c7f67b49ec4', N'ellen', @StaffPasswordHash, N'Ellen', N'ellen@gensan.gov.ph', N'EMP-13080', 4, N'GIS Spatial & Tax Mapping Section', N'09170013080', 1, 2, 19, N'Releasing (GIS)')
        ,('f2f7df57-4360-e664-dcc1-c9c3db0f512f', N'rose', @StaffPasswordHash, N'Rose Marie Bade', N'rose@gensan.gov.ph', N'EMP-14078', 0, N'GIS Spatial & Tax Mapping Section', N'09170014078', 1, 2, 17, N'Preparation (GIS)')
        ,('04c0bfa6-733f-c6fb-2e11-e8c179952647', N'joy', @StaffPasswordHash, N'joy', N'joy@gensan.gov.ph', N'EMP-14079', 5, N'Records & Certification Section', N'09170014079', 1, 3, 28, N'Verification (Records)')
        ,('62c40be9-d923-f9b0-9a8a-6568bd975c81', N'kyle', @StaffPasswordHash, N'Kyle', N'kyle@gensan.gov.ph', N'EMP-14080', 4, N'Records & Certification Section', N'09170014080', 1, 3, 29, N'Approval (Records)')
        ,('9e34d359-19c0-36f4-8b28-abebcf0efd8e', N'judy', @StaffPasswordHash, N'judy', N'judy@gensan.gov.ph', N'EMP-14081', 5, N'Records & Certification Section', N'09170014081', 1, 3, 30, N'Releasing (Records)')
        ,('16dcb85f-a740-6108-cdf1-a4f57eded206', N'nene', @StaffPasswordHash, N'nene', N'nene@gensan.gov.ph', N'EMP-14082', 4, N'GIS Spatial & Tax Mapping Section', N'09170014082', 1, 2, 17, N'Preparation (GIS)')
        ,('2dc999d5-8020-2096-f118-0da9cadbe9cd', N'grace', @StaffPasswordHash, N'grace', N'grace@gensan.gov.ph', N'EMP-15078', 4, N'Records & Certification Section', N'09170015078', 1, 3, 30, N'Releasing (Records)')
        ,('5f863e45-530d-4618-8ade-e0fb092c8a74', N'ayan', @StaffPasswordHash, N'ayan', N'ayan@gensan.gov.ph', N'EMP-16078', 5, N'Real Property Appraisal & Assessment Division', N'09170016078', 1, 1, 2, N'Replotting')
        ,('339870c4-44b1-abaf-a3e8-5dee376d2186', N'rolenber', @StaffPasswordHash, N'rolenber', N'rolenber@gensan.gov.ph', N'EMP-16079', 5, N'Real Property Appraisal & Assessment Division', N'09170016079', 1, 1, 7, N'Valuation & Examination')
        ,('dc2a0350-e1f7-b54b-8ff4-0e5c9b64139a', N'dodong', @StaffPasswordHash, N'dodong', N'dodong@gensan.gov.ph', N'EMP-16080', 1, N'Real Property Appraisal & Assessment Division', N'09170016080', 1, 1, 8, N'Approval')
        ,('2ba9fde2-df2e-04f3-a929-7cd2642152b1', N'printing', @StaffPasswordHash, N'printing', N'printing@gensan.gov.ph', N'EMP-16081', 5, N'Real Property Appraisal & Assessment Division', N'09170016081', 1, 1, 9, N'Printing')
        ,('823060ec-7fd5-c19b-53d0-a086b509628c', N'rc1', @StaffPasswordHash, N'rc1', N'rc1@gensan.gov.ph', N'EMP-17078', 2, N'Real Property Appraisal & Assessment Division', N'09170017078', 1, 1, 1, N'Receiving')
        ,('e4dfb9fe-5d7d-f1aa-c71b-861e77865926', N'markq', @StaffPasswordHash, N'MarkQ', N'markq@gensan.gov.ph', N'EMP-17079', 4, N'Real Property Appraisal & Assessment Division', N'09170017079', 1, 1, 3, N'Inspection')
        ,('3da6fb91-7b6f-bae6-34f8-d9a15667c707', N'ella', @StaffPasswordHash, N'ella', N'ella@gensan.gov.ph', N'EMP-17080', 5, N'Real Property Appraisal & Assessment Division', N'09170017080', 1, 1, 2, N'Replotting')
        ,('b4aa412c-14ed-6785-a754-1f61a2c117d4', N'jerald', @StaffPasswordHash, N'jerald', N'jerald@gensan.gov.ph', N'EMP-17081', 4, N'Real Property Appraisal & Assessment Division', N'09170017081', 1, 1, 6, N'Recommending Approval')
        ,('8e353d75-beff-d058-3481-40dfa3b73e60', N'christine', @StaffPasswordHash, N'christine', N'christine@gensan.gov.ph', N'EMP-17082', 4, N'Real Property Appraisal & Assessment Division', N'09170017082', 1, 1, 7, N'Valuation & Examination')
        ,('c77e150d-d561-7795-b36b-f45a455bd589', N'judie', @StaffPasswordHash, N'judie', N'judie@gensan.gov.ph', N'EMP-17083', 1, N'Real Property Appraisal & Assessment Division', N'09170017083', 1, 1, 8, N'Approval')
        ,('ad1335bd-db38-afca-4ef3-52f9e4f6b815', N'anne', @StaffPasswordHash, N'Anne', N'anne@gensan.gov.ph', N'EMP-17084', 5, N'Real Property Appraisal & Assessment Division', N'09170017084', 1, 1, 9, N'Printing')
        ,('6050e828-a1da-dcd0-94e6-79a74b255901', N'rolen', @StaffPasswordHash, N'rolen', N'rolen@gensan.gov.ph', N'EMP-17085', 5, N'Real Property Appraisal & Assessment Division', N'09170017085', 1, 1, 4, N'Check and Review')
        ,('f06b3ddb-0697-2abc-3fad-a34631bcc463', N'rolen1', @StaffPasswordHash, N'rolen1', N'rolen1@gensan.gov.ph', N'EMP-17086', 5, N'GIS Spatial & Tax Mapping Section', N'09170017086', 1, 2, 18, N'Approval (GIS)')
) AS Source (
    [Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [DivisionId], [WorkflowRoleId], [WorkflowRoleName]
)
ON Target.[Username] = Source.[Username]
WHEN MATCHED THEN
    UPDATE SET
        Target.[FullName] = Source.[FullName],
        Target.[Email] = Source.[Email],
        Target.[Role] = Source.[Role],
        Target.[Department] = Source.[Department],
        Target.[IsActive] = Source.[IsActive],
        Target.[DivisionId] = Source.[DivisionId],
        Target.[WorkflowRoleId] = Source.[WorkflowRoleId],
        Target.[WorkflowRoleName] = Source.[WorkflowRoleName]
WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Id], [Username], [PasswordHash], [FullName], [Email], [EmployeeNo], [Role], [Department], [ContactNo], [IsActive], [CreatedDate], [LastLoginDate], [DivisionId], [WorkflowRoleId], [WorkflowRoleName]
    )
    VALUES (
        Source.[Id], Source.[Username], Source.[PasswordHash], Source.[FullName], Source.[Email], Source.[EmployeeNo], Source.[Role], Source.[Department], Source.[ContactNo], Source.[IsActive], GETDATE(), NULL, Source.[DivisionId], Source.[WorkflowRoleId], Source.[WorkflowRoleName]
    );

PRINT 'Successfully seeded 55 CAST staff user accounts into [dbo].[UserAccounts]!';
GO

-- 3. Verification & Summary Output
SELECT
    COUNT(*) AS [TotalUserAccounts],
    SUM(CASE WHEN DivisionId = 1 THEN 1 ELSE 0 END) AS [Division1_Appraisal],
    SUM(CASE WHEN DivisionId = 2 THEN 1 ELSE 0 END) AS [Division2_TaxMapping],
    SUM(CASE WHEN DivisionId = 3 THEN 1 ELSE 0 END) AS [Division3_Records],
    SUM(CASE WHEN DivisionId IS NULL THEN 1 ELSE 0 END) AS [SuperAdmins],
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS [ActiveUsers],
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS [InactiveUsers]
FROM [dbo].[UserAccounts];
GO
