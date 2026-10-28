-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 19_SeedTransactionStatusesAndWorkflowSequences.sql
-- DESCRIPTION: Seeds official Transaction Statuses and Workflow Sequences 
--              fetched from CAST reference system into [dbo].[TransactionStatus]
--              and [dbo].[WorkflowSequence].
--              Ensures table compatibility, safe idempotent upserts (MERGE),
--              and wires up routing across Divisions 1, 2, and 3.
-- =============================================================================
SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Transaction Statuses & Workflow Sequences Seeding from CAST...';
PRINT '=========================================================================';

-- 1. Ensure TransactionStatus table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TransactionStatus')
BEGIN
    CREATE TABLE [dbo].[TransactionStatus] (
        [TransactionStatusId] INT NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(150) NOT NULL,
        [IsComplete] BIT NOT NULL DEFAULT 0,
        [DivisionId] INT NOT NULL DEFAULT 1
    );
    PRINT 'Created [dbo].[TransactionStatus] table.';
END
GO

-- 2. Seed / Upsert TransactionStatus records fetched from CAST
PRINT 'Seeding [dbo].[TransactionStatus] records...';

MERGE INTO [dbo].[TransactionStatus] AS target
USING (
    VALUES
        -- Division 1: Real Property Appraisal & Tax Assessment Division
        (0,  N'Deferred',                 0, 1),
        (1,  N'Receiving',                0, 1),
        (2,  N'Replotting',               0, 1),
        (3,  N'Inspection',               0, 1),
        (4,  N'Check and Review',         0, 1),
        (5,  N'Repinning',                0, 1),
        (6,  N'Recommending Approval',    0, 1),
        (7,  N'Valuation & Examination',  0, 1),
        (8,  N'Approval',                 0, 1),
        (9,  N'Printing',                 0, 1),
        (10, N'Releasing',                1, 1),

        -- Division 2: GIS Spatial & Tax Mapping Division
        (15, N'Receiving (GIS)',          0, 2),
        (16, N'Replotting (GIS)',         0, 2),
        (17, N'Preparation (GIS)',        0, 2),
        (18, N'Approval (GIS)',           0, 2),
        (19, N'Releasing (GIS)',          1, 2),

        -- Division 3: Records & Tax Declaration Management Division
        (25, N'Receiving (Records)',      0, 3),
        (26, N'Replotting (Records)',     0, 3),
        (27, N'Preparation (Records)',    0, 3),
        (28, N'Verification (Records)',   0, 3),
        (29, N'Approval (Records)',       0, 3),
        (30, N'Releasing (Records)',      1, 3)
) AS source ([TransactionStatusId], [Name], [IsComplete], [DivisionId])
ON target.[TransactionStatusId] = source.[TransactionStatusId]
WHEN MATCHED THEN
    UPDATE SET
        target.[Name] = source.[Name],
        target.[IsComplete] = source.[IsComplete],
        target.[DivisionId] = source.[DivisionId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TransactionStatusId], [Name], [IsComplete], [DivisionId])
    VALUES (source.[TransactionStatusId], source.[Name], source.[IsComplete], source.[DivisionId]);

PRINT 'Seeded ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' records into [dbo].[TransactionStatus].';
GO

-- 3. Ensure WorkflowSequence table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'WorkflowSequence')
BEGIN
    CREATE TABLE [dbo].[WorkflowSequence] (
        [SequenceId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TransactionTypeId] INT NOT NULL,
        [CurrentStatusId] INT NOT NULL,
        [NextStatusId] INT NULL,
        [IsApproval] BIT NOT NULL DEFAULT 0
    );
    PRINT 'Created [dbo].[WorkflowSequence] table.';
END
GO

-- 4. Seed / Upsert WorkflowSequence routes fetched from CAST
PRINT 'Seeding [dbo].[WorkflowSequence] routes...';

-- Temporary table to hold defined sequences
DECLARE @CAST_Sequences TABLE (
    [TransactionTypeId] INT NOT NULL,
    [CurrentStatusId] INT NOT NULL,
    [NextStatusId] INT NULL,
    [IsApproval] BIT NOT NULL
);

INSERT INTO @CAST_Sequences ([TransactionTypeId], [CurrentStatusId], [NextStatusId], [IsApproval])
VALUES
    -- =========================================================================
    -- Transaction Type 1: Request for Inspection
    -- =========================================================================
    (1, 1, 2, 0),      -- Receiving -> Replotting
    (1, 2, 4, 0),      -- Replotting -> Check and Review
    (1, 3, 4, 0),      -- Inspection -> Check and Review
    (1, 4, 5, 0),      -- Check and Review -> Repinning
    (1, 5, 6, 0),      -- Repinning -> Recommending Approval
    (1, 6, 7, 0),      -- Recommending Approval -> Valuation & Examination
    (1, 7, 8, 0),      -- Valuation & Examination -> Approval
    (1, 8, 9, 1),      -- Approval -> Printing (Approval Step = 1)
    (1, 9, 10, 0),     -- Printing -> Releasing
    (1, 10, NULL, 0),  -- Releasing (Final Step)

    -- =========================================================================
    -- Transaction Type 2: Property Assessment
    -- =========================================================================
    (2, 1, 2, 0),      -- Receiving -> Replotting
    (2, 2, 4, 0),      -- Replotting -> Check and Review
    (2, 3, 4, 0),      -- Inspection -> Check and Review
    (2, 4, 5, 0),      -- Check and Review -> Repinning
    (2, 5, 6, 0),      -- Repinning -> Recommending Approval
    (2, 6, 7, 0),      -- Recommending Approval -> Valuation & Examination
    (2, 7, 8, 0),      -- Valuation & Examination -> Approval
    (2, 8, 9, 1),      -- Approval -> Printing (Approval Step = 1)
    (2, 9, 10, 0),     -- Printing -> Releasing
    (2, 10, NULL, 0),  -- Releasing (Final Step)

    -- =========================================================================
    -- Transaction Type 3: Tax Mapping (GIS)
    -- =========================================================================
    (3, 15, 16, 0),    -- Receiving (GIS) -> Replotting (GIS)
    (3, 15, 17, 0),    -- Receiving (GIS) -> Preparation (GIS)
    (3, 16, 18, 0),    -- Replotting (GIS) -> Approval (GIS)
    (3, 17, 18, 0),    -- Preparation (GIS) -> Approval (GIS)
    (3, 18, 19, 1),    -- Approval (GIS) -> Release (GIS) (Approval Step = 1)
    (3, 19, NULL, 0),  -- Release (GIS) (Final Step)

    -- =========================================================================
    -- Transaction Type 4: Tax Declaration (Records)
    -- =========================================================================
    (4, 25, 26, 0),    -- Receiving (Records) -> Replotting (Records)
    (4, 25, 27, 0),    -- Receiving (Records) -> Preparation (Records)
    (4, 26, 28, 0),    -- Replotting (Records) -> Verification (Records)
    (4, 27, 28, 0),    -- Preparation (Records) -> Verification (Records)
    (4, 28, 29, 0),    -- Verification (Records) -> Approval (Records)
    (4, 29, 10, 1),    -- Approval (Records) -> Releasing (Div 1)
    (4, 29, 19, 1),    -- Approval (Records) -> Releasing (GIS)
    (4, 29, 30, 1),    -- Approval (Records) -> Releasing (Records) (Approval Step = 1)
    (4, 30, NULL, 0),  -- Releasing (Records) (Final Step)

    -- =========================================================================
    -- Transaction Type 5: Land Holding (Certifications)
    -- =========================================================================
    (5, 25, 26, 0),    -- Receiving (Records) -> Replotting (Records)
    (5, 26, 27, 0),    -- Replotting (Records) -> Preparation (Records)
    (5, 27, 28, 0),    -- Preparation (Records) -> Verification (Records)
    (5, 28, 29, 0),    -- Verification (Records) -> Approval (Records)
    (5, 29, 10, 1),    -- Approval (Records) -> Releasing (Div 1)
    (5, 29, 19, 1),    -- Approval (Records) -> Releasing (GIS)
    (5, 29, 30, 1),    -- Approval (Records) -> Releasing (Records) (Approval Step = 1)
    (5, 30, NULL, 0);  -- Releasing (Records) (Final Step)

-- Safe insert only if missing
INSERT INTO [dbo].[WorkflowSequence] ([TransactionTypeId], [CurrentStatusId], [NextStatusId], [IsApproval])
SELECT s.[TransactionTypeId], s.[CurrentStatusId], s.[NextStatusId], s.[IsApproval]
FROM @CAST_Sequences s
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[WorkflowSequence] w
    WHERE w.[TransactionTypeId] = s.[TransactionTypeId]
      AND w.[CurrentStatusId] = s.[CurrentStatusId]
      AND ((w.[NextStatusId] IS NULL AND s.[NextStatusId] IS NULL) OR w.[NextStatusId] = s.[NextStatusId])
);

PRINT 'Seeded CAST workflow sequences into [dbo].[WorkflowSequence].';
GO

-- 5. Verification
SELECT 'TransactionStatus' AS [Table], COUNT(*) AS [RowCount] FROM [dbo].[TransactionStatus]
UNION ALL
SELECT 'WorkflowSequence' AS [Table], COUNT(*) AS [RowCount] FROM [dbo].[WorkflowSequence];
GO

PRINT '=========================================================================';
PRINT 'Transaction Statuses & Workflow Sequences Seeding Completed Successfully!';
PRINT '=========================================================================';
