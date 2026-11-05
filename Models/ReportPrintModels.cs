using System;
using System.Collections.Generic;

namespace PropertyAssessmentApp.Models;

/// <summary>
/// Common Header Model for LGU City Assessor official reports and certificates
/// </summary>
public class ReportHeaderModel
{
    public string Country { get; set; } = "Republic of the Philippines";
    public string Province { get; set; } = "Province of South Cotabato";
    public string LguName { get; set; } = "CITY GOVERNMENT OF KORONADAL";
    public string OfficeTitle { get; set; } = "OFFICE OF THE CITY ASSESSOR";
    public string DocumentTitle { get; set; } = "CERTIFICATION";
    public string IntroStatement { get; set; } = "This is to certify that as per record, Tax Declaration Number under General Revision 2005 indicated below was approved by formerly OIC – City Assessor Engr. Arturo R. Buzarang, MPA existed without improvement, to wit:";
    public string? SealImageUrl { get; set; }
}

/// <summary>
/// Individual row item for the common Property Details table
/// </summary>
public class ReportDetailItem
{
    public int No { get; set; } = 1;
    public string TaxDeclarationNo { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string LotNo { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Common Details Table Model
/// </summary>
public class ReportDetailsTableModel
{
    public List<ReportDetailItem> Items { get; set; } = new();
}

/// <summary>
/// Signatory block particulars
/// </summary>
public class ReportSignatoryModel
{
    public string SectionTitle { get; set; } = string.Empty; // e.g. "Verified as per records :", "Noted By:"
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? SubNote { get; set; } // e.g. "Certified as per memorandum dated 03/18/2024" or License No.
}

/// <summary>
/// Common Footer Model for LGU City Assessor official reports and certificates
/// </summary>
public class ReportFooterModel
{
    // Request Details
    public string RequestIntro { get; set; } = "This certification is issued upon request of";
    public string RequestorName { get; set; } = string.Empty;
    public string RequestorAddress { get; set; } = "KORONADAL CITY";
    public string Purpose { get; set; } = "BIR";

    // Date & Location of Issuance
    public string IssueDay { get; set; } = "3RD";
    public string IssueMonth { get; set; } = "NOVEMBER";
    public int IssueYear { get; set; } = 2026;
    public string IssueCityProvince { get; set; } = "City of Koronadal, Province of South Cotabato.";

    // Signatories (matching uploaded official layout)
    public ReportSignatoryModel VerifiedBy { get; set; } = new()
    {
        SectionTitle = "Verified as per records:",
        Name = "ARIANE JANE V. FERNANDEZ, LPT, REA",
        Designation = "Assessment Clerk III"
    };

    public ReportSignatoryModel NotedBy { get; set; } = new()
    {
        SectionTitle = "Noted By:",
        Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
        Designation = "City Assessor License No. 0011516 10/08/2026"
    };

    public ReportSignatoryModel ActingOfficer { get; set; } = new()
    {
        SectionTitle = "As per memorandum dated March 08, 2024",
        Name = "LEONILIE T. ESCARA, LPT, REA",
        Designation = "Tax Mapper III / Acting Records Officer"
    };

    // Fee & Receipt Metadata (Bottom Left)
    public decimal CertFee { get; set; } = 1470.00m;
    public string ORNumber { get; set; } = "3677083";
    public string ORDate { get; set; } = "11/03/2026";

    // LGU Bottom Branding
    public string BrandTitle { get; set; } = "KORONADAL";
    public string BrandPrefix { get; set; } = "LUNGSOD NG";
    public string ContactPhone { get; set; } = "09636387462";
    public string ContactEmail { get; set; } = "cassokoronadalcity.gmail.com";
    public string ContactAddress { get; set; } = "GF, New City Hall Building, Brgy. Zone III, City of Koronadal";
}

/// <summary>
/// Specific model for Certificate of No Improvement (reproducing the exact LGU format from physical sample)
/// </summary>
public class CertificateNoImprovementModel
{
    public string HeaderCountry { get; set; } = "Republic of the Philippines";
    public string HeaderProvince { get; set; } = "Province of South Cotabato";
    public string HeaderLguName { get; set; } = "CITY GOVERNMENT OF KORONADAL";
    public string HeaderOfficeTitle { get; set; } = "OFFICE OF THE CITY ASSESSOR";
    public string DocumentTitle { get; set; } = "CERTIFICATE OF NO IMPROVEMENT";
    public string Salutation { get; set; } = "TO WHOM IT MAY CONCERN";

    // Property Particulars
    public string OwnerName { get; set; } = string.Empty;
    public string PropertyKindOrUse { get; set; } = "AGRICULTURAL";
    public string LotNo { get; set; } = string.Empty;
    public string AreaDisplay { get; set; } = string.Empty; // e.g. "0.1976 has."
    public string LocationDisplay { get; set; } = string.Empty; // e.g. "BRGY. CALOOCAN, City of Koronadal, Province of South Cotabato"
    public string ArpNo { get; set; } = string.Empty; // TDN / ARP NO.

    // Requestor block
    public string RequestorName { get; set; } = string.Empty;
    public string RequestorTin { get; set; } = string.Empty;
    public string RequestorAddress { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    // Date
    public string IssueDay { get; set; } = "3rd";
    public string IssueMonth { get; set; } = "November";
    public int IssueYear { get; set; } = 2026;
    public string IssueLocation { get; set; } = "City of Koronadal, Province of South Cotabato";

    // Signatories matching Koronadal official physical Certificate of No Improvement
    public ReportSignatoryModel ClerkSignatory { get; set; } = new()
    {
        SectionTitle = "Verified as per records :",
        Name = "ARIANE JANE F. GRUPO, LPT, REA",
        Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
    };

    public ReportSignatoryModel AssessorSignatory { get; set; } = new()
    {
        SectionTitle = "Verified as per records :",
        Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
        Designation = "City Assessor License No. 0011516 10/08/2026"
    };

    public ReportSignatoryModel RecordsOfficerSignatory { get; set; } = new()
    {
        SectionTitle = "Certified as per memorandum dated 03/18/2024",
        Name = "LEONILIE T. ESCARA, LPT, REA",
        Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
    };

    // Metadata (Bottom Right)
    public decimal CertFee { get; set; } = 10.00m;
    public string ORNumber { get; set; } = "0009804";
    public string ORDate { get; set; } = "11/03/2026";
    public string DocStampFeeDisplay { get; set; } = "Documentary Stamp Tax Paid Php 30.00";
    public string ControlNumber { get; set; } = "1103202600000000";
    public string PrintedBy { get; set; } = "WHENG";
}

/// <summary>
/// Specific model for "Assessment of Tax Declaration" certification (matching Koronadal City Assessor official layout)
/// </summary>
public class AssessmentOfTaxDeclarationModel
{
    public string HeaderCountry { get; set; } = "Republic of the Philippines";
    public string HeaderProvince { get; set; } = "Province of South Cotabato";
    public string HeaderLguName { get; set; } = "CITY GOVERNMENT OF KORONADAL";
    public string HeaderOfficeTitle { get; set; } = "OFFICE OF THE CITY ASSESSOR";
    public string DocumentTitle { get; set; } = "CERTIFICATION";
    public string Salutation { get; set; } = "TO WHOM IT MAY CONCERN";

    public string TaxDeclarationNo { get; set; } = string.Empty;
    public string LotNo { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TitleNo { get; set; } = string.Empty;
    public string SurveyNo { get; set; } = string.Empty;
    public string Classification { get; set; } = "LAND";
    public string AreaDisplay { get; set; } = string.Empty;
    public decimal MarketValue { get; set; }
    public decimal AssessedValue { get; set; }
    public string Ownership { get; set; } = "Private";

    public string RequestorName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    public string IssueDay { get; set; } = "3RD";
    public string IssueMonth { get; set; } = "November";
    public int IssueYear { get; set; } = 2026;
    public string IssueLocation { get; set; } = "the City of Koronadal, South Cotabato";

    public ReportSignatoryModel ClerkSignatory { get; set; } = new()
    {
        SectionTitle = "Verified by:",
        Name = "ARIANE JANE F. GRUPO, LPT, REA",
        Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
    };

    public ReportSignatoryModel AssessorSignatory { get; set; } = new()
    {
        SectionTitle = "Noted by:",
        Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
        Designation = "City Assessor License No. 0011516 10/08/2026"
    };

    public ReportSignatoryModel RecordsOfficerSignatory { get; set; } = new()
    {
        SectionTitle = "Certified as per memorandum dated 03/18/2024",
        Name = "LEONILIE T. ESCARA, LPT, REA",
        Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
    };

    public string ORNumber { get; set; } = "0009804";
    public string ORDate { get; set; } = "11/03/2026";
    public decimal Amount { get; set; } = 10.00m;
    public string DocStampFeeDisplay { get; set; } = "Documentary Stamp Tax Paid Php 30.00";
}

/// <summary>
/// Row item for structural improvements table in Certificate of Improvement
/// </summary>
public class ImprovementItemModel
{
    public string ArpNo { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string ActualUse { get; set; } = "RES";
    public string Kind { get; set; } = "BLDG";
    public string Area { get; set; } = "37.75 sqm.";
    public decimal MarketValue { get; set; }
    public decimal AssessedValue { get; set; }
    public string YearEffectivity { get; set; } = "2026";
}

/// <summary>
/// Model for "Certificate of Improvement" (reproducing exact physical LGU layout)
/// </summary>
public class CertificateImprovementModel
{
    public string HeaderCountry { get; set; } = "Republic of the Philippines";
    public string HeaderProvince { get; set; } = "Province of South Cotabato";
    public string HeaderLguName { get; set; } = "CITY GOVERNMENT OF KORONADAL";
    public string HeaderOfficeTitle { get; set; } = "OFFICE OF THE CITY ASSESSOR";
    public string DocumentTitle { get; set; } = "CERTIFICATE OF IMPROVEMENT";
    public string Salutation { get; set; } = "TO WHOM IT MAY CONCERN";

    // Land details
    public string OwnerName { get; set; } = string.Empty;
    public string PropertyKindOrUse { get; set; } = "agricultural";
    public string LocationDisplay { get; set; } = string.Empty;
    public string LotNo { get; set; } = string.Empty;
    public string BlockNo { get; set; } = string.Empty;
    public string AreaDisplay { get; set; } = string.Empty;
    public string ArpNo { get; set; } = string.Empty;

    // Structural improvements list
    public List<ImprovementItemModel> Improvements { get; set; } = new();

    // Requestor block
    public string RequestorName { get; set; } = string.Empty;
    public string RequestorTin { get; set; } = string.Empty;
    public string RequestorAddress { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    // Date
    public string IssueDay { get; set; } = "3rd";
    public string IssueMonth { get; set; } = "November";
    public int IssueYear { get; set; } = 2026;
    public string IssueLocation { get; set; } = "the City of Koronadal, Province of South Cotabato";

    // Signatories matching Koronadal official layout
    public ReportSignatoryModel ClerkSignatory { get; set; } = new()
    {
        SectionTitle = "Verified as per records :",
        Name = "ARIANE JANE F. GRUPO, LPT, REA",
        Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
    };

    public ReportSignatoryModel AssessorSignatory { get; set; } = new()
    {
        SectionTitle = "Verified as per records :",
        Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
        Designation = "City Assessor License No. 0011516 10/08/2026"
    };

    public ReportSignatoryModel RecordsOfficerSignatory { get; set; } = new()
    {
        SectionTitle = "Certified as per memorandum dated 03/18/2024",
        Name = "LEONILIE T. ESCARA, LPT, REA",
        Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
    };

    // Metadata (Bottom Right)
    public decimal CertFee { get; set; } = 10.00m;
    public string ORNumber { get; set; } = "0009804";
    public string ORDate { get; set; } = "11/03/2026";
    public string DocStampFeeDisplay { get; set; } = "Documentary Stamp Tax Paid Php 30.00";
    public string ControlNumber { get; set; } = "1103202600000000";
    public string PrintedBy { get; set; } = "WHENG";
}

/// <summary>
/// Model for "Certification of No Land Holding" (reproducing exact physical LGU layout)
/// </summary>
public class CertificationNoLandHoldingModel
{
    public string HeaderCountry { get; set; } = "Republic of the Philippines";
    public string HeaderProvince { get; set; } = "Province of South Cotabato";
    public string HeaderLguName { get; set; } = "CITY GOVERNMENT OF KORONADAL";
    public string HeaderOfficeTitle { get; set; } = "OFFICE OF THE CITY ASSESSOR";
    public string DocumentTitle { get; set; } = "Certification of No Land Holding";
    public string Salutation { get; set; } = "TO WHOM IT MAY CONCERN";

    // Individual particulars
    public string TaxpayerName { get; set; } = string.Empty;
    public string TaxpayerAddress { get; set; } = "KORONADAL CITY";

    // Requestor section
    public string RequestorName { get; set; } = string.Empty;
    public string RequestorTin { get; set; } = string.Empty;
    public string RequestorAddress { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    // Date
    public string IssueDay { get; set; } = "3rd";
    public string IssueMonth { get; set; } = "November";
    public int IssueYear { get; set; } = 2026;
    public string IssueLocation { get; set; } = "the City of Koronadal, Province of South Cotabato";

    // Signatories matching Koronadal official layout
    public ReportSignatoryModel ClerkSignatory { get; set; } = new()
    {
        SectionTitle = "Verified as per records :",
        Name = "ARIANE JANE F. GRUPO, LPT, REA",
        Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
    };

    public ReportSignatoryModel AssessorSignatory { get; set; } = new()
    {
        SectionTitle = "Noted By :",
        Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
        Designation = "City Assessor License No. 0011516 10/08/2026"
    };

    public ReportSignatoryModel RecordsOfficerSignatory { get; set; } = new()
    {
        SectionTitle = "As per memorandum dated 03/18/2024",
        Name = "LEONILIE T. ESCARA, LPT, REA",
        Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
    };

    // Metadata (Bottom Right)
    public decimal CertFee { get; set; } = 65.00m;
    public string ORNumber { get; set; } = "0009804";
    public string ORDate { get; set; } = "11/03/2026";
    public string DocStampFeeDisplay { get; set; } = "Documentary Stamp Tax Paid Php 30.00";
    public string ControlNumber { get; set; } = "1103202600000000";
    public string PrintedBy { get; set; } = "WHENG";
    public string PageDisplay { get; set; } = "Page 1 of 1";
}

/// <summary>
/// Row item for property holdings table in BIR Certification
/// </summary>
public class BirPropertyHoldingItemModel
{
    public string Tdn { get; set; } = string.Empty;
    public string Kind { get; set; } = "R-LAND";
    public string Location { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string TitleNumber { get; set; } = string.Empty;
    public decimal MarketValue { get; set; }
    public decimal AssessedValue { get; set; }
    public string YearComm { get; set; } = "2026";
}

/// <summary>
/// Model for "All Property Holdings (BIR Certification)" (matching physical LGU layout)
/// </summary>
public class AllPropertyHoldingsBirModel
{
    public string HeaderCountry { get; set; } = "Republic of the Philippines";
    public string HeaderProvince { get; set; } = "Province of South Cotabato";
    public string HeaderLguName { get; set; } = "CITY GOVERNMENT OF KORONADAL";
    public string HeaderOfficeTitle { get; set; } = "OFFICE OF THE CITY ASSESSOR";
    public string DocumentTitle { get; set; } = "All Property Holdings (BIR Certification)";
    public string Salutation { get; set; } = "TO WHOM IT MAY CONCERN";

    // Owner particulars
    public string OwnerNames { get; set; } = string.Empty;

    // Table of holdings
    public List<BirPropertyHoldingItemModel> Holdings { get; set; } = new();

    // Requestor section
    public string RequestorName { get; set; } = string.Empty;
    public string RequestorTin { get; set; } = string.Empty;
    public string RequestorAddress { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;

    // Date
    public string IssueDay { get; set; } = "3rd";
    public string IssueMonth { get; set; } = "November";
    public int IssueYear { get; set; } = 2026;
    public string IssueLocation { get; set; } = "the City of Koronadal, Province of South Cotabato";

    // Signatories matching Koronadal official layout
    public ReportSignatoryModel ClerkSignatory { get; set; } = new()
    {
        SectionTitle = "Verified as per records :",
        Name = "ARIANE JANE F. GRUPO, LPT, REA",
        Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
    };

    public ReportSignatoryModel AssessorSignatory { get; set; } = new()
    {
        SectionTitle = "Noted By :",
        Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
        Designation = "City Assessor License No. 0011516 10/08/2026"
    };

    public ReportSignatoryModel RecordsOfficerSignatory { get; set; } = new()
    {
        SectionTitle = "As per memorandum dated 03/18/2024",
        Name = "LEONILIE T. ESCARA, LPT, REA",
        Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
    };

    // Metadata (Bottom Right)
    public decimal CertFee { get; set; } = 65.00m;
    public string ORNumber { get; set; } = "0009804";
    public string ORDate { get; set; } = "11/03/2026";
    public string DocStampFeeDisplay { get; set; } = "Documentary Stamp Tax Paid Php 30.00";
    public string ControlNumber { get; set; } = "1103202600000000";
    public string PrintedBy { get; set; } = "WHENG";
    public string PageDisplay { get; set; } = "Page 1 of 1";
}

/// <summary>
/// Model for "Agricultural Lands (DAR Certification)" / Official Assessment Certification
/// matching the exact physical layout in the uploaded sample
/// </summary>
public class AgriculturalDarCertificationModel
{
    public string HeaderCountry { get; set; } = "Republic of the Philippines";
    public string HeaderProvince { get; set; } = "Province of South Cotabato";
    public string HeaderLguName { get; set; } = "CITY GOVERNMENT OF KORONADAL";
    public string HeaderOfficeTitle { get; set; } = "OFFICE OF THE CITY ASSESSOR";
    public string DocumentTitle { get; set; } = "CERTIFICATION";
    public string Salutation { get; set; } = "TO WHOM IT MAY CONCERN:";

    public string TaxDeclarationNo { get; set; } = "018-00406";
    public string LotNo { get; set; } = "1152 (1142-B)";
    public string OwnerName { get; set; } = "Brgy. Government of New Pangasinan";
    public string LocationLine1 { get; set; } = "Purok Golden Shower, Brgy. New Pangasinan";
    public string LocationLine2 { get; set; } = "Koronadal City";
    public string TitleNo { get; set; } = "N/A";
    public string SurveyNo { get; set; } = "GSS-131-D";
    public string Classification { get; set; } = "Agricultural";
    public string AreaDisplay { get; set; } = "11,441.00 sqm.";
    public decimal MarketValue { get; set; } = 0.00m;
    public decimal AssessedValue { get; set; } = 0.00m;

    public string RequestorName { get; set; } = "SAHAR CARUMBA-KADDORA";
    public string Purpose { get; set; } = "REFERENCE";

    public string IssueDay { get; set; } = "20th";
    public string IssueMonth { get; set; } = "July";
    public int IssueYear { get; set; } = 2026;
    public string IssueLocation { get; set; } = "the City of Koronadal, South Cotabato";

    // Official Signatories
    public ReportSignatoryModel ClerkSignatory { get; set; } = new()
    {
        SectionTitle = "Verified as per records:",
        Name = "PAULINE ANNE GRACE R. CRUZ, REB, REA",
        Designation = "Administrative Assistant V (DEMO III)",
        SubNote = "License No. 0013049 Valid until 04/02/2028"
    };

    public ReportSignatoryModel AssessorSignatory { get; set; } = new()
    {
        SectionTitle = "Noted by:",
        Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
        Designation = "City Assessor License No. 0011516 10/08/2026"
    };

    public ReportSignatoryModel RecordsOfficerSignatory { get; set; } = new()
    {
        SectionTitle = "As per memorandum dated March 18, 2024",
        Name = "LEONILIE T. ESCARA, LPT",
        Designation = "Tax Mapper III / Acting Record Officer"
    };

    // Metadata (Bottom-Left)
    public string ReceiptDate { get; set; } = "07/20/26";
    public decimal Amount { get; set; } = 150.00m;
    public string ORNumber { get; set; } = "3633033";

    // Bottom LGU Branding
    public string BrandTitle { get; set; } = "KORONADAL";
    public string BrandPrefix { get; set; } = "LUNGSOD NG";
    public string ContactPhone { get; set; } = "09636387462";
    public string ContactEmail { get; set; } = "cassokoronadalcity.gmail.com";
    public string ContactAddress { get; set; } = "GF, New City Hall Building, Brgy. Zone III, City of Koronadal";
}

/// <summary>
/// Composite model for easy end-to-end rendering
/// </summary>
public class OfficialCertificateViewModel
{
    public ReportHeaderModel Header { get; set; } = new();
    public ReportDetailsTableModel Details { get; set; } = new();
    public ReportFooterModel Footer { get; set; } = new();
    public CertificateNoImprovementModel? NoImprovementDetails { get; set; }
    public AssessmentOfTaxDeclarationModel? AssessmentDetails { get; set; }
    public CertificateImprovementModel? ImprovementDetails { get; set; }
    public CertificationNoLandHoldingModel? NoLandHoldingDetails { get; set; }
    public AllPropertyHoldingsBirModel? BirHoldingsDetails { get; set; }
    public AgriculturalDarCertificationModel? DarDetails { get; set; }
    public int ReportId { get; set; }
    public string RawReportTitle { get; set; } = string.Empty;

    public bool IsNoImprovementCertificate =>
        ReportId == 2 ||
        (RawReportTitle?.Contains("NO Improvement", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (RawReportTitle?.Contains("Certificate of NO Improvement", StringComparison.OrdinalIgnoreCase) ?? false);

    public bool IsAssessmentOfTdCertificate =>
        ReportId == 3 ||
        (RawReportTitle?.Contains("Assessment of Tax Declaration", StringComparison.OrdinalIgnoreCase) ?? false);

    public bool IsImprovementCertificate =>
        ReportId == 4 ||
        (!IsNoImprovementCertificate && (RawReportTitle?.Contains("Certificate of Improvement", StringComparison.OrdinalIgnoreCase) ?? false));

    public bool IsNoLandHoldingCertificate =>
        ReportId == 5 ||
        (RawReportTitle?.Contains("no land holding", StringComparison.OrdinalIgnoreCase) ?? false);

    public bool IsBirCertification =>
        ReportId == 6 ||
        (RawReportTitle?.Contains("BIR", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (RawReportTitle?.Contains("All property Holding", StringComparison.OrdinalIgnoreCase) ?? false);

    public bool IsDarCertification =>
        ReportId == 7 ||
        (RawReportTitle?.Contains("DAR", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (RawReportTitle?.Contains("Agricultural Lands", StringComparison.OrdinalIgnoreCase) ?? false);
}
