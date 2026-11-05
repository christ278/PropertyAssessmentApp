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
    public string SectionTitle { get; set; } = string.Empty; // e.g. "Verified as per records:", "Noted By:"
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? SubNote { get; set; } // e.g. "As per memorandum dated March 08, 2024" or License No.
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
/// Composite model for easy end-to-end rendering
/// </summary>
public class OfficialCertificateViewModel
{
    public ReportHeaderModel Header { get; set; } = new();
    public ReportDetailsTableModel Details { get; set; } = new();
    public ReportFooterModel Footer { get; set; } = new();
    public int ReportId { get; set; }
    public string RawReportTitle { get; set; } = string.Empty;
}
