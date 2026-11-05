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
    public static string ToOrdinalDay(int day, bool lowerCase = false)
    {
        if (day <= 0) return lowerCase ? $"{day}th" : $"{day}TH";

        int rem100 = day % 100;
        string suffix;
        if (rem100 is 11 or 12 or 13)
        {
            suffix = lowerCase ? "th" : "TH";
        }
        else
        {
            suffix = (day % 10) switch
            {
                1 => lowerCase ? "st" : "ST",
                2 => lowerCase ? "nd" : "ND",
                3 => lowerCase ? "rd" : "RD",
                _ => lowerCase ? "th" : "TH"
            };
        }

        return $"{day}{suffix}";
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

        var title = report.Title ?? string.Empty;
        var now = DateTime.Now;

        // 1. Build Header
        model.Header.Country = "Republic of the Philippines";
        model.Header.Province = "Province of South Cotabato";
        model.Header.LguName = "CITY GOVERNMENT OF KORONADAL";
        model.Header.OfficeTitle = "OFFICE OF THE CITY ASSESSOR";
        model.Header.DocumentTitle = model.IsNoImprovementCertificate 
            ? "CERTIFICATE OF NO IMPROVEMENT" 
            : "CERTIFICATION";

        // Determine specific certification intro statement
        if (model.IsNoImprovementCertificate)
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

        // 2. Build Details Table Items (for tabular certificate layout)
        var locationStr = FormatLocation(td);
        var lotNoStr = string.IsNullOrWhiteSpace(td.LotNo) ? "168-B-5" : td.LotNo.Trim();

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
        model.Footer.RequestorName = (string.IsNullOrWhiteSpace(requestor) ? (td.OwnerName ?? string.Empty) : requestor).ToUpper();
        model.Footer.RequestorAddress = "KORONADAL CITY";
        model.Footer.Purpose = string.IsNullOrWhiteSpace(purpose) ? "BIR" : purpose.Trim().ToUpper();

        model.Footer.IssueDay = ToOrdinalDay(now.Day);
        model.Footer.IssueMonth = now.ToString("MMMM", CultureInfo.InvariantCulture).ToUpper();
        model.Footer.IssueYear = now.Year;
        model.Footer.IssueCityProvince = "City of Koronadal, Province of South Cotabato.";

        model.Footer.CertFee = certFee ?? (model.IsNoImprovementCertificate ? 10.00m : 1470.00m);
        model.Footer.ORNumber = string.IsNullOrWhiteSpace(orNumber) ? (model.IsNoImprovementCertificate ? "0009804" : "3677083") : orNumber.Trim();
        model.Footer.ORDate = now.ToString("MM/dd/yyyy");

        // Standard official Koronadal Assessor signatories
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

        // 4. Build Specific Certificate of No Improvement model if applicable
        if (model.IsNoImprovementCertificate)
        {
            string areaDisplay;
            if (td.LandDetails != null && td.LandDetails.AreaHectares > 0)
            {
                areaDisplay = $"{td.LandDetails.AreaHectares:0.####} has.";
            }
            else if (td.AreaSqMeters >= 10000)
            {
                areaDisplay = $"{(td.AreaSqMeters / 10000m):0.####} has.";
            }
            else if (td.AreaSqMeters > 0)
            {
                areaDisplay = $"{td.AreaSqMeters:N0} sq.m.";
            }
            else
            {
                areaDisplay = "0.1976 has.";
            }

            var kindDisplay = !string.IsNullOrWhiteSpace(td.ActualUse) 
                ? td.ActualUse.ToUpper() 
                : (!string.IsNullOrWhiteSpace(td.LandDetails?.ActualUse)
                    ? td.LandDetails.ActualUse.ToUpper()
                    : (td.Kind == PropertyKind.Land ? "AGRICULTURAL" : td.Kind.ToString().ToUpper()));

            model.NoImprovementDetails = new CertificateNoImprovementModel
            {
                HeaderCountry = "Republic of the Philippines",
                HeaderProvince = "Province of South Cotabato",
                HeaderLguName = "CITY GOVERNMENT OF KORONADAL",
                HeaderOfficeTitle = "OFFICE OF THE CITY ASSESSOR",
                DocumentTitle = "CERTIFICATE OF NO IMPROVEMENT",
                Salutation = "TO WHOM IT MAY CONCERN",

                OwnerName = (td.OwnerName ?? string.Empty).ToUpper(),
                PropertyKindOrUse = kindDisplay,
                LotNo = lotNoStr,
                AreaDisplay = areaDisplay,
                LocationDisplay = locationStr.ToUpper(),
                ArpNo = td.TaxDeclarationNumber,

                RequestorName = string.IsNullOrWhiteSpace(requestor) ? (td.OwnerName ?? string.Empty).ToUpper() : requestor.Trim().ToUpper(),
                RequestorTin = td.OwnerTin ?? string.Empty,
                RequestorAddress = "KORONADAL CITY",
                Purpose = string.IsNullOrWhiteSpace(purpose) ? string.Empty : purpose.Trim().ToUpper(),

                IssueDay = ToOrdinalDay(now.Day, lowerCase: true),
                IssueMonth = now.ToString("MMMM", CultureInfo.InvariantCulture),
                IssueYear = now.Year,
                IssueLocation = "the City of Koronadal, Province of South Cotabato",

                ClerkSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Verified as per records :",
                    Name = "ARIANE JANE F. GRUPO, LPT, REA",
                    Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
                },

                AssessorSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Verified as per records :",
                    Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
                    Designation = "City Assessor License No. 0011516 10/08/2026"
                },

                RecordsOfficerSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Certified as per memorandum dated 03/18/2024",
                    Name = "LEONILIE T. ESCARA, LPT, REA",
                    Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
                },

                CertFee = certFee ?? 10.00m,
                ORNumber = string.IsNullOrWhiteSpace(orNumber) ? "0009804" : orNumber.Trim(),
                ORDate = now.ToString("MM/dd/yyyy"),
                DocStampFeeDisplay = "Documentary Stamp Tax Paid Php 30.00",
                ControlNumber = $"{now:MMddyyyy}00000000",
                PrintedBy = string.IsNullOrWhiteSpace(requestor) ? "WHENG" : requestor.Trim().ToUpper()
            };
        }

        // 5. Build Specific Assessment of Tax Declaration model if applicable
        if (model.IsAssessmentOfTdCertificate)
        {
            string areaDisplay;
            if (td.LandDetails != null && td.LandDetails.AreaHectares > 0)
            {
                areaDisplay = $"{td.LandDetails.AreaHectares:0.####} has.";
            }
            else if (td.AreaSqMeters >= 10000)
            {
                areaDisplay = $"{(td.AreaSqMeters / 10000m):0.####} has.";
            }
            else if (td.AreaSqMeters > 0)
            {
                areaDisplay = $"{td.AreaSqMeters:N0} sq.m.";
            }
            else
            {
                areaDisplay = "0.1976 has.";
            }

            var purok = !string.IsNullOrWhiteSpace(td.Purok) ? td.Purok.Trim().ToUpper() : "PUROK PAG-ASA";
            var brgyName = !string.IsNullOrWhiteSpace(td.DistrictOrBarangay) ? td.DistrictOrBarangay.Trim().ToUpper() : "BRGY. CALOOCAN";
            if (!brgyName.StartsWith("BRGY", StringComparison.OrdinalIgnoreCase))
            {
                brgyName = $"BRGY. {brgyName}";
            }
            var assessmentLocation = $"{purok}, {brgyName}";

            var classificationStr = td.Kind == PropertyKind.Land ? "LAND" : (!string.IsNullOrWhiteSpace(td.ActualUse) ? td.ActualUse.ToUpper() : td.Kind.ToString().ToUpper());

            model.AssessmentDetails = new AssessmentOfTaxDeclarationModel
            {
                HeaderCountry = "Republic of the Philippines",
                HeaderProvince = "Province of South Cotabato",
                HeaderLguName = "CITY GOVERNMENT OF KORONADAL",
                HeaderOfficeTitle = "OFFICE OF THE CITY ASSESSOR",
                DocumentTitle = "CERTIFICATION",
                Salutation = "TO WHOM IT MAY CONCERN",

                TaxDeclarationNo = td.TaxDeclarationNumber,
                LotNo = lotNoStr,
                OwnerName = (td.OwnerName ?? string.Empty).ToUpper(),
                Location = assessmentLocation,
                TitleNo = td.TitleNo ?? td.LandDetails?.TitleNo ?? string.Empty,
                SurveyNo = td.SurveyNo ?? td.LandDetails?.SurveyNo ?? string.Empty,
                Classification = classificationStr,
                AreaDisplay = areaDisplay,
                MarketValue = td.MarketValue > 0 ? td.MarketValue : (td.AdjustedMarketValue > 0 ? td.AdjustedMarketValue : 50348.48m),
                AssessedValue = td.AssessedValue > 0 ? td.AssessedValue : 7550.00m,
                Ownership = td.Taxability?.Equals("Exempt", StringComparison.OrdinalIgnoreCase) == true ? "Government" : "Private",

                RequestorName = string.IsNullOrWhiteSpace(requestor) ? string.Empty : requestor.Trim().ToUpper(),
                Purpose = string.IsNullOrWhiteSpace(purpose) ? string.Empty : purpose.Trim().ToUpper(),

                IssueDay = ToOrdinalDay(now.Day, lowerCase: false),
                IssueMonth = now.ToString("MMMM", CultureInfo.InvariantCulture),
                IssueYear = now.Year,
                IssueLocation = "the City of Koronadal, South Cotabato",

                ClerkSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Verified by:",
                    Name = "ARIANE JANE F. GRUPO, LPT, REA",
                    Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
                },

                AssessorSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Noted by:",
                    Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
                    Designation = "City Assessor License No. 0011516 10/08/2026"
                },

                RecordsOfficerSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Certified as per memorandum dated 03/18/2024",
                    Name = "LEONILIE T. ESCARA, LPT, REA",
                    Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
                },

                ORNumber = string.IsNullOrWhiteSpace(orNumber) ? "0009804" : orNumber.Trim(),
                ORDate = now.ToString("MM/dd/yyyy"),
                Amount = certFee ?? 10.00m,
                DocStampFeeDisplay = "Documentary Stamp Tax Paid Php 30.00"
            };
        }

        // 6. Build Specific Certificate of Improvement model if applicable
        if (model.IsImprovementCertificate)
        {
            string areaDisplay;
            if (td.LandDetails != null && td.LandDetails.AreaHectares > 0)
            {
                areaDisplay = $"{td.LandDetails.AreaHectares:0.####} has.";
            }
            else if (td.AreaSqMeters >= 10000)
            {
                areaDisplay = $"{(td.AreaSqMeters / 10000m):0.####} has.";
            }
            else if (td.AreaSqMeters > 0)
            {
                areaDisplay = $"{td.AreaSqMeters:N0} sq.m.";
            }
            else
            {
                areaDisplay = "1.4999 has.";
            }

            var brgy = string.IsNullOrWhiteSpace(td.DistrictOrBarangay) ? "BRGY. MAGSAYSAY" : (td.DistrictOrBarangay.StartsWith("Brgy", StringComparison.OrdinalIgnoreCase) ? td.DistrictOrBarangay.ToUpper() : $"BRGY. {td.DistrictOrBarangay.ToUpper()}");
            var city = string.IsNullOrWhiteSpace(td.Municipality) ? "City of KORONADAL" : (td.Municipality.StartsWith("City", StringComparison.OrdinalIgnoreCase) ? td.Municipality : $"City of {td.Municipality.ToUpper()}");
            var locDisplay = $"{brgy}, {city}";

            var improvementsList = new List<ImprovementItemModel>();
            if (relatedProperties != null && relatedProperties.Any())
            {
                foreach (var rel in relatedProperties)
                {
                    improvementsList.Add(new ImprovementItemModel
                    {
                        ArpNo = rel.TaxDeclarationNumber,
                        OwnerName = (rel.OwnerName ?? td.OwnerName ?? string.Empty).ToUpper(),
                        ActualUse = !string.IsNullOrWhiteSpace(rel.ActualUse) ? (rel.ActualUse.Length > 3 ? rel.ActualUse.Substring(0, 3).ToUpper() : rel.ActualUse.ToUpper()) : "RES",
                        Kind = rel.Kind == PropertyKind.Building ? "BLDG" : (rel.Kind == PropertyKind.Machinery ? "MACH" : "BLDG"),
                        Area = rel.AreaSqMeters > 0 ? $"{rel.AreaSqMeters:0.##} sqm." : "37.75 sqm.",
                        MarketValue = rel.MarketValue > 0 ? rel.MarketValue : (rel.AdjustedMarketValue > 0 ? rel.AdjustedMarketValue : 112008.00m),
                        AssessedValue = rel.AssessedValue,
                        YearEffectivity = rel.EffectivityYear > 0 ? rel.EffectivityYear.ToString() : now.Year.ToString()
                    });
                }
            }

            if (!improvementsList.Any())
            {
                if (td.Kind == PropertyKind.Building || td.BuildingDetails != null)
                {
                    improvementsList.Add(new ImprovementItemModel
                    {
                        ArpNo = td.TaxDeclarationNumber,
                        OwnerName = (td.OwnerName ?? string.Empty).ToUpper(),
                        ActualUse = "RES",
                        Kind = "BLDG",
                        Area = td.AreaSqMeters > 0 ? $"{td.AreaSqMeters:0.##} sqm." : "37.75 sqm.",
                        MarketValue = td.MarketValue > 0 ? td.MarketValue : 112008.00m,
                        AssessedValue = td.AssessedValue,
                        YearEffectivity = td.EffectivityYear > 0 ? td.EffectivityYear.ToString() : now.Year.ToString()
                    });
                }
                else
                {
                    improvementsList.Add(new ImprovementItemModel
                    {
                        ArpNo = "11-0014-01168",
                        OwnerName = "LUCENA CABANGCALA JOVENAL",
                        ActualUse = "RES",
                        Kind = "BLDG",
                        Area = "37.75 sqm.",
                        MarketValue = 112008.00m,
                        AssessedValue = 0.00m,
                        YearEffectivity = "2017"
                    });
                    improvementsList.Add(new ImprovementItemModel
                    {
                        ArpNo = "11-0014-01171",
                        OwnerName = "DANDY C. JOVINAL",
                        ActualUse = "RES",
                        Kind = "BLDG",
                        Area = "15.00 sqm.",
                        MarketValue = 72930.00m,
                        AssessedValue = 0.00m,
                        YearEffectivity = "2027"
                    });
                }
            }

            model.ImprovementDetails = new CertificateImprovementModel
            {
                HeaderCountry = "Republic of the Philippines",
                HeaderProvince = "Province of South Cotabato",
                HeaderLguName = "CITY GOVERNMENT OF KORONADAL",
                HeaderOfficeTitle = "OFFICE OF THE CITY ASSESSOR",
                DocumentTitle = "CERTIFICATE OF IMPROVEMENT",
                Salutation = "TO WHOM IT MAY CONCERN",

                OwnerName = (td.OwnerName ?? string.Empty).ToUpper(),
                PropertyKindOrUse = td.Kind == PropertyKind.Land ? "agricultural" : (!string.IsNullOrWhiteSpace(td.ActualUse) ? td.ActualUse.ToLower() : "agricultural"),
                LocationDisplay = locDisplay,
                LotNo = string.IsNullOrWhiteSpace(td.LotNo) ? "461-F" : td.LotNo.Trim(),
                BlockNo = string.IsNullOrWhiteSpace(td.BlockNo) ? "Blk" : $"Blk {td.BlockNo.Trim()}",
                AreaDisplay = areaDisplay,
                ArpNo = td.TaxDeclarationNumber,

                Improvements = improvementsList,

                RequestorName = string.IsNullOrWhiteSpace(requestor) ? string.Empty : requestor.Trim().ToUpper(),
                RequestorTin = td.OwnerTin ?? string.Empty,
                RequestorAddress = string.IsNullOrWhiteSpace(td.OwnerAddress) ? string.Empty : td.OwnerAddress.Trim().ToUpper(),
                Purpose = string.IsNullOrWhiteSpace(purpose) ? string.Empty : purpose.Trim().ToUpper(),

                IssueDay = ToOrdinalDay(now.Day, lowerCase: true),
                IssueMonth = now.ToString("MMMM", CultureInfo.InvariantCulture),
                IssueYear = now.Year,
                IssueLocation = "the City of Koronadal, Province of South Cotabato",

                ClerkSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Verified as per records :",
                    Name = "ARIANE JANE F. GRUPO, LPT, REA",
                    Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
                },

                AssessorSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Verified as per records :",
                    Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
                    Designation = "City Assessor License No. 0011516 10/08/2026"
                },

                RecordsOfficerSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Certified as per memorandum dated 03/18/2024",
                    Name = "LEONILIE T. ESCARA, LPT, REA",
                    Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
                },

                CertFee = certFee ?? 10.00m,
                ORNumber = string.IsNullOrWhiteSpace(orNumber) ? "0009804" : orNumber.Trim(),
                ORDate = now.ToString("MM/dd/yyyy"),
                DocStampFeeDisplay = "Documentary Stamp Tax Paid Php 30.00",
                ControlNumber = $"{now:MMddyyyy}00000000",
                PrintedBy = string.IsNullOrWhiteSpace(requestor) ? "WHENG" : requestor.Trim().ToUpper()
            };
        }

        // 7. Build Specific Certification of No Land Holding model if applicable
        if (model.IsNoLandHoldingCertificate)
        {
            var targetName = !string.IsNullOrWhiteSpace(requestor) 
                ? requestor.Trim().ToUpper() 
                : (!string.IsNullOrWhiteSpace(td.OwnerName) ? td.OwnerName.ToUpper() : "NAME");

            var targetAddress = !string.IsNullOrWhiteSpace(td.OwnerAddress) 
                ? td.OwnerAddress.Trim().ToUpper() 
                : "KORONADAL CITY";

            model.NoLandHoldingDetails = new CertificationNoLandHoldingModel
            {
                HeaderCountry = "Republic of the Philippines",
                HeaderProvince = "Province of South Cotabato",
                HeaderLguName = "CITY GOVERNMENT OF KORONADAL",
                HeaderOfficeTitle = "OFFICE OF THE CITY ASSESSOR",
                DocumentTitle = "Certification of No Land Holding",
                Salutation = "TO WHOM IT MAY CONCERN",

                TaxpayerName = targetName,
                TaxpayerAddress = targetAddress,

                RequestorName = string.IsNullOrWhiteSpace(requestor) ? string.Empty : requestor.Trim().ToUpper(),
                RequestorTin = td.OwnerTin ?? string.Empty,
                RequestorAddress = targetAddress,
                Purpose = string.IsNullOrWhiteSpace(purpose) ? string.Empty : purpose.Trim().ToUpper(),

                IssueDay = ToOrdinalDay(now.Day, lowerCase: true),
                IssueMonth = now.ToString("MMMM", CultureInfo.InvariantCulture),
                IssueYear = now.Year,
                IssueLocation = "the City of Koronadal, Province of South Cotabato",

                ClerkSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Verified as per records :",
                    Name = "ARIANE JANE F. GRUPO, LPT, REA",
                    Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
                },

                AssessorSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Noted By :",
                    Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
                    Designation = "City Assessor License No. 0011516 10/08/2026"
                },

                RecordsOfficerSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "As per memorandum dated 03/18/2024",
                    Name = "LEONILIE T. ESCARA, LPT, REA",
                    Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
                },

                CertFee = certFee ?? 65.00m,
                ORNumber = string.IsNullOrWhiteSpace(orNumber) ? "0009804" : orNumber.Trim(),
                ORDate = now.ToString("MM/dd/yyyy"),
                DocStampFeeDisplay = "Documentary Stamp Tax Paid Php 30.00",
                ControlNumber = $"{now:MMddyyyy}00000000",
                PrintedBy = string.IsNullOrWhiteSpace(requestor) ? "WHENG" : requestor.Trim().ToUpper(),
                PageDisplay = "Page 1 of 1"
            };
        }

        // 8. Build Specific All Property Holdings (BIR Certification) model if applicable
        if (model.IsBirCertification)
        {
            var holdingsList = new List<BirPropertyHoldingItemModel>();

            var allProps = new List<TaxDeclaration>();
            if (relatedProperties != null && relatedProperties.Any())
            {
                allProps.AddRange(relatedProperties);
                if (!allProps.Any(p => p.TaxDeclarationNumber == td.TaxDeclarationNumber))
                {
                    allProps.Insert(0, td);
                }
            }
            else
            {
                allProps.Add(td);
            }

            foreach (var item in allProps)
            {
                string kindStr = "R-LAND";
                if (item.Kind == PropertyKind.Land)
                {
                    kindStr = item.Classification switch
                    {
                        PropertyClassification.Commercial => "C-LAND",
                        PropertyClassification.Industrial => "I-LAND",
                        PropertyClassification.Agricultural => "A-LAND",
                        _ => "R-LAND"
                    };
                }
                else if (item.Kind == PropertyKind.Building)
                {
                    kindStr = "BLDG";
                }
                else
                {
                    kindStr = "MACH";
                }

                var locStr = !string.IsNullOrWhiteSpace(item.DistrictOrBarangay)
                    ? item.DistrictOrBarangay.Replace("Brgy.", "").Replace("Brgy", "").Trim().ToUpper()
                    : "CONCEPCION";

                var lotStr = !string.IsNullOrWhiteSpace(item.LotNo)
                    ? (item.LotNo.StartsWith("Lot", StringComparison.OrdinalIgnoreCase) ? item.LotNo : $"Lot {item.LotNo}")
                    : "Lot 431-D-8";

                string areaStr;
                if (item.AreaSqMeters > 0)
                {
                    areaStr = $"{item.AreaSqMeters:N2} sqm.";
                }
                else if (item.LandDetails != null && item.LandDetails.AreaHectares > 0)
                {
                    areaStr = $"{item.LandDetails.AreaHectares:0.####} has.";
                }
                else
                {
                    areaStr = "534.00 sqm.";
                }

                var titleStr = !string.IsNullOrWhiteSpace(item.TitleNo)
                    ? item.TitleNo
                    : (!string.IsNullOrWhiteSpace(item.LandDetails?.TitleNo) ? item.LandDetails.TitleNo : "145-2025006144");

                holdingsList.Add(new BirPropertyHoldingItemModel
                {
                    Tdn = item.TaxDeclarationNumber,
                    Kind = kindStr,
                    Location = locStr,
                    LotNumber = lotStr,
                    Area = areaStr,
                    TitleNumber = titleStr,
                    MarketValue = item.MarketValue > 0 ? item.MarketValue : (item.AdjustedMarketValue > 0 ? item.AdjustedMarketValue : 137450.00m),
                    AssessedValue = item.AssessedValue > 0 ? item.AssessedValue : 20620.00m,
                    YearComm = item.EffectivityYear > 0 ? item.EffectivityYear.ToString() : now.Year.ToString()
                });
            }

            var ownerDisplay = !string.IsNullOrWhiteSpace(td.OwnerName)
                ? td.OwnerName.ToUpper()
                : "APOSTOL, MARIA RHEA CONNIE R. AND RANDY SPS.";

            model.BirHoldingsDetails = new AllPropertyHoldingsBirModel
            {
                HeaderCountry = "Republic of the Philippines",
                HeaderProvince = "Province of South Cotabato",
                HeaderLguName = "CITY GOVERNMENT OF KORONADAL",
                HeaderOfficeTitle = "OFFICE OF THE CITY ASSESSOR",
                DocumentTitle = "All Property Holdings (BIR Certification)",
                Salutation = "TO WHOM IT MAY CONCERN",

                OwnerNames = ownerDisplay,
                Holdings = holdingsList,

                RequestorName = string.IsNullOrWhiteSpace(requestor) ? string.Empty : requestor.Trim().ToUpper(),
                RequestorTin = td.OwnerTin ?? string.Empty,
                RequestorAddress = string.IsNullOrWhiteSpace(td.OwnerAddress) ? string.Empty : td.OwnerAddress.Trim().ToUpper(),
                Purpose = string.IsNullOrWhiteSpace(purpose) ? string.Empty : purpose.Trim().ToUpper(),

                IssueDay = ToOrdinalDay(now.Day, lowerCase: true),
                IssueMonth = now.ToString("MMMM", CultureInfo.InvariantCulture),
                IssueYear = now.Year,
                IssueLocation = "the City of Koronadal, Province of South Cotabato",

                ClerkSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Verified as per records :",
                    Name = "ARIANE JANE F. GRUPO, LPT, REA",
                    Designation = "Assessment Clerk III License No. 0014426 05/19/2029"
                },

                AssessorSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Noted By :",
                    Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
                    Designation = "City Assessor License No. 0011516 10/08/2026"
                },

                RecordsOfficerSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "As per memorandum dated 03/18/2024",
                    Name = "LEONILIE T. ESCARA, LPT, REA",
                    Designation = "Tax Mapper III / Acting Records Officer License No. 0014412 08/09/2029"
                },

                CertFee = certFee ?? 65.00m,
                ORNumber = string.IsNullOrWhiteSpace(orNumber) ? "0009804" : orNumber.Trim(),
                ORDate = now.ToString("MM/dd/yyyy"),
                DocStampFeeDisplay = "Documentary Stamp Tax Paid Php 30.00",
                ControlNumber = $"{now:MMddyyyy}00000000",
                PrintedBy = string.IsNullOrWhiteSpace(requestor) ? "WHENG" : requestor.Trim().ToUpper(),
                PageDisplay = "Page 1 of 1"
            };
        }

        // 9. Build Specific Agricultural Lands (DAR Certification) model if applicable
        if (model.IsDarCertification)
        {
            var purok = !string.IsNullOrWhiteSpace(td.Purok) ? td.Purok.Trim() : "Purok Golden Shower";
            var brgy = !string.IsNullOrWhiteSpace(td.DistrictOrBarangay) ? td.DistrictOrBarangay.Trim() : "Brgy. New Pangasinan";
            if (!brgy.StartsWith("Brgy", StringComparison.OrdinalIgnoreCase))
            {
                brgy = $"Brgy. {brgy}";
            }

            string areaDisplay;
            if (td.AreaSqMeters > 0)
            {
                areaDisplay = $"{td.AreaSqMeters:N2} sqm.";
            }
            else if (td.LandDetails != null && td.LandDetails.AreaHectares > 0)
            {
                areaDisplay = $"{td.LandDetails.AreaHectares:0.####} has.";
            }
            else
            {
                areaDisplay = "11,441.00 sqm.";
            }

            var titleStr = !string.IsNullOrWhiteSpace(td.TitleNo) 
                ? td.TitleNo 
                : (!string.IsNullOrWhiteSpace(td.LandDetails?.TitleNo) ? td.LandDetails.TitleNo : "N/A");

            var surveyStr = !string.IsNullOrWhiteSpace(td.SurveyNo) 
                ? td.SurveyNo 
                : (!string.IsNullOrWhiteSpace(td.LandDetails?.SurveyNo) ? td.LandDetails.SurveyNo : "GSS-131-D");

            var classStr = td.Kind == PropertyKind.Land 
                ? (td.Classification == PropertyClassification.Agricultural ? "Agricultural" : td.Classification.ToString()) 
                : "Agricultural";

            var reqName = !string.IsNullOrWhiteSpace(requestor) ? requestor.Trim().ToUpper() : "SAHAR CARUMBA-KADDORA";
            var reqPurpose = !string.IsNullOrWhiteSpace(purpose) ? purpose.Trim().ToUpper() : "REFERENCE";

            model.DarDetails = new AgriculturalDarCertificationModel
            {
                HeaderCountry = "Republic of the Philippines",
                HeaderProvince = "Province of South Cotabato",
                HeaderLguName = "CITY GOVERNMENT OF KORONADAL",
                HeaderOfficeTitle = "OFFICE OF THE CITY ASSESSOR",
                DocumentTitle = "CERTIFICATION",
                Salutation = "TO WHOM IT MAY CONCERN:",

                TaxDeclarationNo = td.TaxDeclarationNumber,
                LotNo = string.IsNullOrWhiteSpace(td.LotNo) ? "1152 (1142-B)" : td.LotNo.Trim(),
                OwnerName = !string.IsNullOrWhiteSpace(td.OwnerName) ? td.OwnerName.Trim() : "Brgy. Government of New Pangasinan",
                LocationLine1 = $"{purok}, {brgy}",
                LocationLine2 = "Koronadal City",
                TitleNo = titleStr,
                SurveyNo = surveyStr,
                Classification = classStr,
                AreaDisplay = areaDisplay,
                MarketValue = td.MarketValue,
                AssessedValue = td.AssessedValue,

                RequestorName = reqName,
                Purpose = reqPurpose,

                IssueDay = ToOrdinalDay(now.Day, lowerCase: true),
                IssueMonth = now.ToString("MMMM", CultureInfo.InvariantCulture),
                IssueYear = now.Year,
                IssueLocation = "the City of Koronadal, South Cotabato",

                ClerkSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Verified as per records:",
                    Name = "PAULINE ANNE GRACE R. CRUZ, REB, REA",
                    Designation = "Administrative Assistant V (DEMO III)",
                    SubNote = "License No. 0013049 Valid until 04/02/2028"
                },

                AssessorSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "Noted by:",
                    Name = "MARIA RHEA CONNIE R. APOSTOL, MPA, REA",
                    Designation = "City Assessor License No. 0011516 10/08/2026"
                },

                RecordsOfficerSignatory = new ReportSignatoryModel
                {
                    SectionTitle = "As per memorandum dated March 18, 2024",
                    Name = "LEONILIE T. ESCARA, LPT",
                    Designation = "Tax Mapper III / Acting Record Officer"
                },

                ReceiptDate = now.ToString("MM/dd/yy"),
                Amount = certFee ?? 150.00m,
                ORNumber = string.IsNullOrWhiteSpace(orNumber) ? "3633033" : orNumber.Trim(),

                BrandTitle = "KORONADAL",
                BrandPrefix = "LUNGSOD NG",
                ContactPhone = "09636387462",
                ContactEmail = "cassokoronadalcity.gmail.com",
                ContactAddress = "GF, New City Hall Building, Brgy. Zone III, City of Koronadal"
            };
        }

        return model;
    }

    private static string FormatLocation(TaxDeclaration td)
    {
        var brgy = string.IsNullOrWhiteSpace(td.DistrictOrBarangay) ? "Brgy. Caloocan" : td.DistrictOrBarangay;
        if (!brgy.StartsWith("Brgy.", StringComparison.OrdinalIgnoreCase))
        {
            brgy = $"Brgy. {brgy}";
        }

        var city = string.IsNullOrWhiteSpace(td.Municipality) ? "City of Koronadal" : td.Municipality;
        if (!city.StartsWith("City", StringComparison.OrdinalIgnoreCase))
        {
            city = $"City of {city}";
        }

        var province = string.IsNullOrWhiteSpace(td.ProvinceCity) ? "Province of South Cotabato" : td.ProvinceCity;
        if (!province.StartsWith("Province", StringComparison.OrdinalIgnoreCase) && !province.Contains("Cotabato", StringComparison.OrdinalIgnoreCase))
        {
            province = $"Province of {province}";
        }
        else if (province.Equals("South Cotabato", StringComparison.OrdinalIgnoreCase))
        {
            province = "Province of South Cotabato";
        }

        return $"{brgy}, {city}, {province}";
    }
}
