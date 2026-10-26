const fs = require('fs');
const items = require('./parsed_additional_items.json');

// 1. Generate C# seed lines
const csLines = items.map(x => {
    const escapedDesc = x.desc.replace(/"/g, '\\"');
    return `                new BuildingAdditionalItem { Id = ${x.id}, AdditionalItemDescription = "${escapedDesc}", PercentUnitValue = ${x.percent.toFixed(2)}m, CostPerSQM = ${x.cost.toFixed(2)}m, ExcessSQM = ${x.excessSqm.toFixed(2)}m, ExcessPercentage = ${x.excessPct.toFixed(2)}m, RevisionYear = ${x.revYear}, IsActive = true, SortOrder = ${x.id} }`;
});

fs.writeFileSync('C:/Users/HP_02/Desktop/PropertyAssessmentApp/SqlScripts/csharp_seeds.txt', csLines.join(',\r\n'));

// 2. Generate Idempotent SQL script
const sqlValues = items.map(x => {
    const escapedDesc = x.desc.replace(/'/g, "''");
    return `    (${x.id}, N'${escapedDesc}', ${x.percent.toFixed(2)}, ${x.cost.toFixed(2)}, ${x.excessSqm.toFixed(2)}, ${x.excessPct.toFixed(2)}, ${x.revYear}, 1, ${x.id})`;
}).join(',\r\n');

const sqlScript = `-- ==============================================================================
-- Seed Script: Standard Building Additional Items (CAST System Alignment)
-- Description: Inserts standard additional items / improvements reference data (52 items)
-- Target Table: dbo.BuildingAdditionalItems
-- ==============================================================================

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.BuildingAdditionalItems', N'U') IS NOT NULL
BEGIN
    PRINT 'Seeding reference data into dbo.BuildingAdditionalItems...';

    SET IDENTITY_INSERT [dbo].[BuildingAdditionalItems] ON;

    MERGE INTO [dbo].[BuildingAdditionalItems] AS target
    USING (VALUES
${sqlValues}
    ) AS source (
        [Id], [AdditionalItemDescription], [PercentUnitValue], [CostPerSQM],
        [ExcessSQM], [ExcessPercentage], [RevisionYear], [IsActive], [SortOrder]
    )
    ON target.[Id] = source.[Id]
    WHEN MATCHED THEN
        UPDATE SET
            target.[AdditionalItemDescription] = source.[AdditionalItemDescription],
            target.[PercentUnitValue] = source.[PercentUnitValue],
            target.[CostPerSQM] = source.[CostPerSQM],
            target.[ExcessSQM] = source.[ExcessSQM],
            target.[ExcessPercentage] = source.[ExcessPercentage],
            target.[RevisionYear] = source.[RevisionYear],
            target.[IsActive] = source.[IsActive],
            target.[SortOrder] = source.[SortOrder]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [AdditionalItemDescription], [PercentUnitValue], [CostPerSQM], [ExcessSQM], [ExcessPercentage], [RevisionYear], [IsActive], [SortOrder])
        VALUES (source.[Id], source.[AdditionalItemDescription], source.[PercentUnitValue], source.[CostPerSQM], source.[ExcessSQM], source.[ExcessPercentage], source.[RevisionYear], source.[IsActive], source.[SortOrder]);

    SET IDENTITY_INSERT [dbo].[BuildingAdditionalItems] OFF;

    PRINT 'dbo.BuildingAdditionalItems seeding complete. Total reference items seeded: 52.';
END
ELSE
BEGIN
    PRINT 'Table dbo.BuildingAdditionalItems does not exist yet. Please run EF Core migration first.';
END
GO
`;

fs.writeFileSync('C:/Users/HP_02/Desktop/PropertyAssessmentApp/SqlScripts/10_SeedBuildingAdditionalItems.sql', sqlScript);
console.log('Generated C# seeds and 10_SeedBuildingAdditionalItems.sql successfully!');
