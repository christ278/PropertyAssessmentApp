using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class RenameLandPrerequisiteTablesToLandRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. refClassification -> LandRefClassification
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refClassification')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefClassification')
                BEGIN
                    EXEC sp_rename 'dbo.refClassification', 'LandRefClassification';
                END

                -- 2. refSubClassification -> LandRefSubClassification
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refSubClassification')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefSubClassification')
                BEGIN
                    EXEC sp_rename 'dbo.refSubClassification', 'LandRefSubClassification';
                END

                -- 3. refActualUse -> LandRefActualUse
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refActualUse')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefActualUse')
                BEGIN
                    EXEC sp_rename 'dbo.refActualUse', 'LandRefActualUse';
                END

                -- 4. refLandAssessmentLevel -> LandRefAssessmentLevel
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refLandAssessmentLevel')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefAssessmentLevel')
                BEGIN
                    EXEC sp_rename 'dbo.refLandAssessmentLevel', 'LandRefAssessmentLevel';
                END

                -- 5. refAdjustment -> LandRefAdjustment
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refAdjustment')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefAdjustment')
                BEGIN
                    EXEC sp_rename 'dbo.refAdjustment', 'LandRefAdjustment';
                END

                -- 6. refImprovementType -> LandRefImprovementType
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refImprovementType')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefImprovementType')
                BEGIN
                    EXEC sp_rename 'dbo.refImprovementType', 'LandRefImprovementType';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefClassification')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refClassification')
                BEGIN
                    EXEC sp_rename 'dbo.LandRefClassification', 'refClassification';
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefSubClassification')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refSubClassification')
                BEGIN
                    EXEC sp_rename 'dbo.LandRefSubClassification', 'refSubClassification';
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefActualUse')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refActualUse')
                BEGIN
                    EXEC sp_rename 'dbo.LandRefActualUse', 'refActualUse';
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefAssessmentLevel')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refLandAssessmentLevel')
                BEGIN
                    EXEC sp_rename 'dbo.LandRefAssessmentLevel', 'refLandAssessmentLevel';
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefAdjustment')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refAdjustment')
                BEGIN
                    EXEC sp_rename 'dbo.LandRefAdjustment', 'refAdjustment';
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'LandRefImprovementType')
                   AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'refImprovementType')
                BEGIN
                    EXEC sp_rename 'dbo.LandRefImprovementType', 'refImprovementType';
                END
            ");
        }
    }
}
