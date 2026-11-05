using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Helpers;

public static class ReportPrintHelper
{
    /// <summary>
    /// Converts a day of month integer into uppercase ordinal string (e.g. 1 -> "1ST", 2 -> "2ND", 3 -> "3RD", 4 -> "4TH")
    /// </summary>
    public static string ToOrdinalDay(int day)
    {
        if (day <= 0) return $"{day}TH";

        int rem100 = day % 100;
        if (rem100 is 11 or 12 or 13)
        {
            return $"{day}TH";
        }

        return (day % 10) switch
        {
            1 => $"{day}ST",
            2 => $"{day}ND",
            3 => $"{day}RD",
            _ => $"{day}TH"
        };
    }

    /// <summary>
    /// Factory method building an OfficialCertificateViewModel from a TaxDeclaration and RPTReport
    /// matching the exact formatting of the City Assessor's official printed certification.
    /// </summary>
    public static OfficialCertificateViewModel BuildCertificateModel(
        TaxDeclaration td,
        RPTReport report,
        string? requestor = null,
        string? purpose = null,
        string? orNumber = null,
        decimal? certFee = null,
        IEnumerable<TaxDeclaration>? relatedProperties = null)
    {
        var model = new OfficialCertificateViewModel
        {
            ReportId = report.RPTReportId,
            RawReportTitle = report.Title
        };

        // 1. Build Header
        model.Header.Country = "Republic of the Philippines";
        model.Header.Province = "Province of South Cotabato";
        model.Header.LguName = "CITY GOVERNMENT OF KORONADAL";
        model.Header.OfficeTitle = "OFFICE OF THE CITY ASSESSOR";
        model.Header.DocumentTitle = "CERTIFICATION";

        // Determine specific certification intro statement
        var title = report.Title ?? string.Empty;
        if (title.Contains("NO Improvement", StringComparison.OrdinalIgnoreCase))
        {
            model.Header.IntroStatement = "This is to certify that as per record, Tax Declaration Number under General Revision 2005 indicated below was approved by formerly OIC – City Assessor Engr. Arturo R. Buzarang, MPA existed without improvement, to wit:";
        }
        else if (title.Contains("Improvement", StringComparison.OrdinalIgnoreCase))
        {
            model.Header.IntroStatement = "This is to certify that as per record and ocular appraisal on file in this office, the structural improvements and buildings constructed on the parcel indicated below are recorded as follows, to wit:";
        }
        else if (title.Contains("no land holding", StringComparison.OrdinalIgnoreCase))
        {
            model.Header.IntroStatement = "This is to certify that as per the master database and cadastral tax mapping records of this office, the person indicated below has no registered real property land holding in this City, to wit:";
        }
        else if (title.Contains("BIR", StringComparison.OrdinalIgnoreCase))
        {
            model.Header.IntroStatement = "This is to certify for the Bureau of Internal Revenue (BIR) that the real property holdings registered under the name of the taxpayer indicated below are recorded as follows, to wit:";
        }
        else if (title.Contains("DAR", StringComparison.OrdinalIgnoreCase))
        {
            model.Header.IntroStatement = "This is to certify for the Department of Agrarian Reform (DAR) that the agricultural landholdings indicated below have been verified against the cadastral records of this office, to wit:";
        }
        else if (title.Contains("Assessment", StringComparison.OrdinalIgnoreCase))
        {
            model.Header.IntroStatement = "This is to certify that as per the official assessment records of this office, the property indicated below is declared for taxation purposes with the corresponding valuation details, to wit:";
        }
        else
        {
            model.Header.IntroStatement = "This is to certify that as per record on file in this office, the Tax Declaration Number indicated below has been verified and authenticated against the master assessment roll, to wit:";
        }

        // 2. Build Details Table Items
        var locationStr = FormatLocation(td);
        var lotNoStr = string.IsNullOrWhiteSpace(td.LotNo) ? "N/A" : td.LotNo.Trim();

        var primaryItem = new ReportDetailItem
        {
            No = 1,
            TaxDeclarationNo = td.TaxDeclarationNumber,
            OwnerName = (td.OwnerName ?? string.Empty).ToUpper(),
            LotNo = lotNoStr,
            Location = locationStr
        };

        model.Details.Items.Add(primaryItem);

        // If multi-holding certification and related properties were loaded, include them
        if (relatedProperties != null)
        {
            int rowNo = 2;
            foreach (var rel in relatedProperties.Where(r => r.TaxDeclarationNumber != td.TaxDeclarationNumber))
            {
                model.Details.Items.Add(new ReportDetailItem
                {
                    No = rowNo++,
                    TaxDeclarationNo = rel.TaxDeclarationNumber,
                    OwnerName = (rel.OwnerName ?? td.OwnerName ?? string.Empty).ToUpper(),
                    LotNo = string.IsNullOrWhiteSpace(rel.LotNo) ? "N/A" : rel.LotNo.Trim(),
                    Location = FormatLocation(rel)
                });
            }
        }

        // 3. Build Footer
        var now = DateTime.Now;
        model.Footer.RequestorName = (string.IsNullOrWhiteSpace(requestor) ? (td.OwnerName ?? string.Empty) : requestor).ToUpper();
        model.Footer.RequestorAddress = "KORONADAL CITY";
        model.Footer.Purpose = string.IsNullOrWhiteSpace(purpose) ? "BIR" : purpose.Trim().ToUpper();

        model.Footer.IssueDay = ToOrdinalDay(now.Day);
        model.Footer.IssueMonth = now.ToString("MMMM", CultureInfo.InvariantCulture).ToUpper();
        model.Footer.IssueYear = now.Year;
        model.Footer.IssueCityProvince = "City of Koronadal, Province of South Cotabato.";

        model.Footer.CertFee = certFee ?? 1470.00m;
        model.Footer.ORNumber = string.IsNullOrWhiteSpace(orNumber) ? "3677083" : orNumber.Trim();
        model.Footer.ORDate = now.ToString("MM/dd/yyyy");

        // Standard official Koronadal Assessor signatories from the template
        model.Footer.VerifiedBy = new ReportSignatoryModel
        {
            SectionTitle = "Verified as per records:",
            Name = "ARIANE JANE V. FERNANDEZ, LPT, REA",
            Designation = "Assessment Clerk III"
        };

        model.Footer.NotedBy = new ReportSignatoryModel
        {
            SectionTitle = "Noted By:",
            Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
            Designation = "City Assessor License No. 0011516 10/08/2026"
        };

        model.Footer.ActingOfficer = new ReportSignatoryModel
        {
            SectionTitle = "As per memorandum dated March 08, 2024",
            Name = "LEONILIE T. ESCARA, LPT, REA",
            Designation = "Tax Mapper III / Acting Records Officer"
        };

        return model;
    }

    private static string FormatLocation(TaxDeclaration td)
    {
        var brgy = string.IsNullOrWhiteSpace(td.DistrictOrBarangay) ? "Brgy. Zone III" : td.DistrictOrBarangay;
        if (!brgy.StartsWith("Brgy.", StringComparison.OrdinalIgnoreCase))
        {
            brgy = $"Brgy. {brgy}";
        }

        var city = string.IsNullOrWhiteSpace(td.Municipality) ? "City of Koronadal" : td.Municipality;
        if (!city.StartsWith("City", StringComparison.OrdinalIgnoreCase))
        {
            city = $"City of {city}";
        }

        var province = string.IsNullOrWhiteSpace(td.ProvinceCity) ? "South Cotabato" : td.ProvinceCity;

        return $"{brgy}, {city}, {province}";
    }
}
