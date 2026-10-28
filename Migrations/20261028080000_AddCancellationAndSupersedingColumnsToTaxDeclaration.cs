using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PropertyAssessmentApp.Data;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261028080000_AddCancellationAndSupersedingColumnsToTaxDeclaration")]
    public partial class AddCancellationAndSupersedingColumnsToTaxDeclaration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('TaxDeclarations', 'IsCancelledAndSuperseded') IS NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] ADD [IsCancelledAndSuperseded] bit NOT NULL DEFAULT CAST(0 AS bit);
                END;

                IF COL_LENGTH('TaxDeclarations', 'CancellationReason') IS NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] ADD [CancellationReason] nvarchar(500) NULL;
                END;

                IF COL_LENGTH('TaxDeclarations', 'DateCancelled') IS NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] ADD [DateCancelled] datetime2 NULL;
                END;

                IF COL_LENGTH('TaxDeclarations', 'SupersededByTdns') IS NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] ADD [SupersededByTdns] nvarchar(max) NULL;
                END;

                IF COL_LENGTH('TaxDeclarations', 'CancelledByRequestId') IS NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] ADD [CancelledByRequestId] uniqueidentifier NULL;
                END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('TaxDeclarations', 'IsCancelledAndSuperseded') IS NOT NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] DROP COLUMN [IsCancelledAndSuperseded];
                END;
                IF COL_LENGTH('TaxDeclarations', 'CancellationReason') IS NOT NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] DROP COLUMN [CancellationReason];
                END;
                IF COL_LENGTH('TaxDeclarations', 'DateCancelled') IS NOT NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] DROP COLUMN [DateCancelled];
                END;
                IF COL_LENGTH('TaxDeclarations', 'SupersededByTdns') IS NOT NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] DROP COLUMN [SupersededByTdns];
                END;
                IF COL_LENGTH('TaxDeclarations', 'CancelledByRequestId') IS NOT NULL
                BEGIN
                    ALTER TABLE [TaxDeclarations] DROP COLUMN [CancelledByRequestId];
                END;
            ");
        }
    }
}
