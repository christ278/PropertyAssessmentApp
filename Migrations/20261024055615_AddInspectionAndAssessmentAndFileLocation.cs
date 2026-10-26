using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionAndAssessmentAndFileLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[FileLocation]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FileLocation] (
        [FileLocationId] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Location] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_FileLocation] PRIMARY KEY ([FileLocationId])
    );
END

IF OBJECT_ID(N'[dbo].[PropertyAssessmentRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PropertyAssessmentRequests] (
        [Id] uniqueidentifier NOT NULL,
        [TrackingNumber] nvarchar(max) NOT NULL,
        [TransactionTypeCode] nvarchar(max) NOT NULL,
        [TransactionTypeName] nvarchar(max) NOT NULL,
        [Kind] nvarchar(max) NOT NULL,
        [Partitions] int NOT NULL,
        [RefTaxDeclarationId] uniqueidentifier NULL,
        [PrevTDN] nvarchar(max) NOT NULL,
        [PrevPIN] nvarchar(max) NOT NULL,
        [PrevOwner] nvarchar(max) NOT NULL,
        [LotNo] nvarchar(max) NOT NULL,
        [PropertyLocation] nvarchar(max) NOT NULL,
        [NewOwnerName] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [OwnerTIN] nvarchar(max) NULL,
        [ContactNo] nvarchar(max) NULL,
        [EmailAddress] nvarchar(max) NULL,
        [Requestor] nvarchar(max) NULL,
        [DocTitleTctCloa] bit NOT NULL,
        [DocDeedOfSaleConveyance] bit NOT NULL,
        [DocCarBir] bit NOT NULL,
        [DocTaxClearance] bit NOT NULL,
        [DocTransferTaxReceipt] bit NOT NULL,
        [DocApprovedSurveyPlan] bit NOT NULL,
        [DocCadastralMap] bit NOT NULL,
        [DocDenrCert] bit NOT NULL,
        [DocBuildingPermit] bit NOT NULL,
        [DocCertCompletion] bit NOT NULL,
        [DocCertOccupancy] bit NOT NULL,
        [DocInvoiceReceipts] bit NOT NULL,
        [DocInstallationCostBreakdown] bit NOT NULL,
        [DocSpecsManual] bit NOT NULL,
        [DocSpaAuthLetter] bit NOT NULL,
        [DocValidIdOwnerRep] bit NOT NULL,
        [DocOthers] bit NOT NULL,
        [DocOthersSpecify] nvarchar(max) NULL,
        [RequestedDocumentsJson] nvarchar(max) NULL,
        [TotalFeesAmount] decimal(18,2) NOT NULL,
        [ORNumber] nvarchar(max) NULL,
        [ORDateIssued] datetime2 NULL,
        [AttachmentFileName] nvarchar(max) NULL,
        [AttachmentFileSize] nvarchar(max) NULL,
        [PhysicalStoragePath] nvarchar(max) NULL,
        [WebRelativePath] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [AssignedAppraiser] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [RequestDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PropertyAssessmentRequests] PRIMARY KEY ([Id])
    );
END

IF OBJECT_ID(N'[dbo].[RequestForInspections]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RequestForInspections] (
        [Id] uniqueidentifier NOT NULL,
        [TrackingNumber] nvarchar(max) NOT NULL,
        [TransactionCode] nvarchar(max) NULL,
        [TransactionNo] nvarchar(max) NULL,
        [IsDepreciation] bit NOT NULL,
        [IsReassessment] bit NOT NULL,
        [IsCancelled] bit NOT NULL,
        [IsOther] bit NOT NULL,
        [OtherPurposeDetails] nvarchar(max) NULL,
        [TaxDeclarationId] uniqueidentifier NULL,
        [TaxDeclarationNumber] nvarchar(max) NOT NULL,
        [PropertyIndexNumber] nvarchar(max) NOT NULL,
        [OwnerName] nvarchar(max) NOT NULL,
        [Kind] nvarchar(max) NOT NULL,
        [LotNo] nvarchar(max) NOT NULL,
        [PropertyLocation] nvarchar(max) NOT NULL,
        [ContactPerson] nvarchar(max) NULL,
        [ContactNumber] nvarchar(max) NULL,
        [EmailAddress] nvarchar(max) NULL,
        [Representative] nvarchar(max) NULL,
        [RequestedDocumentsJson] nvarchar(max) NULL,
        [TotalFeesAmount] decimal(18,2) NOT NULL,
        [ORNumber] nvarchar(max) NULL,
        [ORDateIssued] datetime2 NULL,
        [AttachmentFileName] nvarchar(max) NULL,
        [AttachmentFileSize] nvarchar(max) NULL,
        [PhysicalStoragePath] nvarchar(max) NULL,
        [WebRelativePath] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [AssignedStaff] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [RequestDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_RequestForInspections] PRIMARY KEY ([Id])
    );
END

IF OBJECT_ID(N'[dbo].[FileLocation]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[FileLocation] WHERE [FileLocationId] = 1)
    BEGIN
        SET IDENTITY_INSERT [dbo].[FileLocation] ON;
        INSERT INTO [dbo].[FileLocation] ([FileLocationId], [Location], [Name]) VALUES
        (1, '\\\\192.168.0.182\\mapping_system\\Attachments\\UserProfile', 'Profile'),
        (2, '\\\\192.168.0.182\\mapping_system\\Attachments\\Attachment _for_Testing', 'Attachment'),
        (3, '\\\\192.168.0.182\\mapping_system\\Attachments\\Skech', 'Sketch'),
        (4, '\\\\192.168.0.182\\mapping_system\\Attachments\\Inspection', 'InspectionAttachment'),
        (5, '\\\\192.168.0.182\\mapping_system\\Attachments\\Inspection', 'SketchLand'),
        (6, '\\\\192.168.0.182\\mapping_system\\Attachments\\PropertyAssessment', 'PropertyAssessment'),
        (11, '~/UploadFile/UserProfile', 'Profile'),
        (12, '~/UploadFile/Attachment', 'Attachment'),
        (13, '~/UploadFile/Sketch', 'Sketch'),
        (14, '~/UploadFile/Inspection', 'InspectionAttachmentNetwork'),
        (15, '~/UploadFile/Land', 'SketchLandBase'),
        (16, '~/UploadFile/PropertyAssessment', 'PropertyAssessmentBase');
        SET IDENTITY_INSERT [dbo].[FileLocation] OFF;
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileLocation");

            migrationBuilder.DropTable(
                name: "PropertyAssessmentRequests");

            migrationBuilder.DropTable(
                name: "RequestForInspections");
        }
    }
}
