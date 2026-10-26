using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxDeclarationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxDeclarationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxDeclarationNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PropertyLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequesterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ORNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ORDateIssued = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Purposes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReqIdOfOwner = table.Column<bool>(type: "bit", nullable: false),
                    ReqIdOfRepresentative = table.Column<bool>(type: "bit", nullable: false),
                    ReqAuthorizationLetter = table.Column<bool>(type: "bit", nullable: false),
                    ReqDeedOfConveyance = table.Column<bool>(type: "bit", nullable: false),
                    ReqCertIndigency = table.Column<bool>(type: "bit", nullable: false),
                    CertTaxDeclaration = table.Column<bool>(type: "bit", nullable: false),
                    CertRecordsVerification = table.Column<bool>(type: "bit", nullable: false),
                    CertTaxDecTimeOfDeath = table.Column<bool>(type: "bit", nullable: false),
                    CertAnnotationBailBond = table.Column<bool>(type: "bit", nullable: false),
                    CertPropertyHistory = table.Column<bool>(type: "bit", nullable: false),
                    CertAnnotationMortgages = table.Column<bool>(type: "bit", nullable: false),
                    CertNoImprovement = table.Column<bool>(type: "bit", nullable: false),
                    CertCancellationAnnotation = table.Column<bool>(type: "bit", nullable: false),
                    CertSupportingDocuments = table.Column<bool>(type: "bit", nullable: false),
                    RequestedDocumentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalFeesAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentFileSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhysicalStoragePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebRelativePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDeclarationRequests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxDeclarationRequests");
        }
    }
}
