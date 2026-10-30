using System.Text.Json;
using PropertyAssessmentApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PropertyAssessmentApp.Services;

public interface ITaxDeclarationPdfService
{
    /// <summary>
    /// Official 1-page Tax Declaration Certificate (used for Property Assessment Requests &amp; Inspection Requests).
    /// </summary>
    byte[] GenerateTaxDeclarationPdf(TaxDeclaration td);

    /// <summary>
    /// Multi-page Official Tax Declaration Certificate for a Request (1 page per partition / TD).
    /// </summary>
    byte[] GenerateRequestTdCertificatePdf(IReadOnlyList<TaxDeclaration> declarations, string? trackingNo = null, string? transactionLabel = null);

    /// <summary>
    /// Complete FAAS &amp; Tax Declaration Master Record PDF containing ALL fields and ALL tables for Land, Building, or Machinery (used in Assessment Records).
    /// </summary>
    byte[] GenerateCompleteRecordPdf(TaxDeclaration td);
}

public class TaxDeclarationPdfService : ITaxDeclarationPdfService
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public byte[] GenerateTaxDeclarationPdf(TaxDeclaration td)
    {
        return GenerateRequestTdCertificatePdf(new[] { td });
    }

    public byte[] GenerateRequestTdCertificatePdf(IReadOnlyList<TaxDeclaration> declarations, string? trackingNo = null, string? transactionLabel = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

        var items = declarations != null && declarations.Count > 0
            ? declarations
            : new[] { new TaxDeclaration() };

        var document = Document.Create(container =>
        {
            foreach (var td in items)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Legal);
                    page.MarginVertical(24);
                    page.MarginHorizontal(28);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Colors.Grey.Darken4));

                    page.Header().Element(c => ComposeCertificateHeader(c, td, trackingNo, transactionLabel));
                    page.Content().Element(c => ComposeCertificateContent(c, td));
                    page.Footer().Element(c => ComposeFooter(c, td));
                });
            }
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateCompleteRecordPdf(TaxDeclaration td)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Legal);
                page.MarginVertical(20);
                page.MarginHorizontal(26);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8f).FontColor(Colors.Grey.Darken4));

                if (td.Kind == PropertyKind.Land)
                {
                    page.Header().Element(c => ComposeLandAnnexIeHeader(c, td));
                    page.Content().Element(c => ComposeLandAnnexIeContent(c, td));
                    page.Footer().Element(c => ComposeFooter(c, td));
                }
                else if (td.Kind == PropertyKind.Building)
                {
                    page.Header().Element(c => ComposeBuildingAnnexIfHeader(c, td));
                    page.Content().Element(c => ComposeBuildingAnnexIfContent(c, td));
                    page.Footer().Element(c => ComposeFooter(c, td));
                }
                else
                {
                    page.Header().Element(c => ComposeMasterRecordHeader(c, td));
                    page.Content().Element(c => ComposeMasterRecordContent(c, td));
                    page.Footer().Element(c => ComposeFooter(c, td));
                }
            });
        });

        return document.GeneratePdf();
    }

    // =========================================================================
    // PART 1: COMPLETE FAAS & TAX DECLARATION MASTER RECORD (ALL FIELDS & TABLES)
    // =========================================================================
    private static void ComposeMasterRecordHeader(IContainer container, TaxDeclaration td)
    {
        container.PaddingBottom(6).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("REPUBLIC OF THE PHILIPPINES • LOCAL GOVERNMENT UNIT").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text($"OFFICE OF THE CITY ASSESSOR — {ValueOr(td.Municipality, "General Santos City").ToUpperInvariant()}").FontSize(11).ExtraBold().FontColor(Colors.Black);
                    c.Item().Text($"REAL PROPERTY FAAS & TAX DECLARATION MASTER RECORD ({td.Kind.ToString().ToUpperInvariant()})").FontSize(10).Black().FontColor(Colors.Teal.Darken3);
                });

                row.AutoItem().AlignRight().Column(c =>
                {
                    var statusLabel = (td.IsCancelledAndSuperseded || td.Status == AssessmentStatus.Cancelled)
                        ? "CANCELLED & SUPERSEDED"
                        : td.Status.ToString().ToUpperInvariant();
                    c.Item().Border(1).BorderColor(Colors.Grey.Darken2).Background(Colors.Grey.Lighten4).PaddingHorizontal(8).PaddingVertical(4).Column(sc =>
                    {
                        sc.Item().AlignRight().Text($"TD NO.: {ValueOr(td.TaxDeclarationNumber, "N/A")}").FontSize(10).Black();
                        sc.Item().AlignRight().Text($"PIN: {ValueOr(td.PropertyIndexNumber, "N/A")}").FontSize(8).Bold();
                        sc.Item().AlignRight().Text($"STATUS: {statusLabel}").FontSize(7.5f).ExtraBold().FontColor(
                            statusLabel.Contains("CANCEL") ? Colors.Red.Darken2 : Colors.Teal.Darken3);
                    });
                });
            });

            if (td.IsCancelledAndSuperseded || td.Status == AssessmentStatus.Cancelled)
            {
                col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Red.Medium).Background(Colors.Red.Lighten5).Padding(5).Row(r =>
                {
                    var reason = string.IsNullOrWhiteSpace(td.CancellationReason) ? "" : $" due to {td.CancellationReason}";
                    var dateStr = (td.DateCancelled ?? td.DateAssessed).ToString("MMMM dd, yyyy");
                    var supStr = string.IsNullOrWhiteSpace(td.SupersededByTdns) ? "" : $" | Superseded by TD(s): {td.SupersededByTdns}";
                    r.RelativeItem().AlignCenter().Text($"*** CANCELLED & SUPERSEDED on {dateStr}{reason}{supStr} ***").FontSize(8).Bold().FontColor(Colors.Red.Darken3);
                });
            }
        });
    }

    private static void ComposeMasterRecordContent(IContainer container, TaxDeclaration td)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            // Top Executive Summary Strip
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Background(Colors.Grey.Lighten4).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                KeyValueCell(t, "Property Kind", td.Kind.ToString().ToUpperInvariant());
                KeyValueCell(t, "ARP No.", ValueOr(td.ARPNo, "N/A"));
                KeyValueCell(t, "Transaction Code", ValueOr(td.TransactionCodeDisplay, td.TransactionCode));
                KeyValueCell(t, "Market Value (FMV)", $"PHP {td.MarketValue:N2}");
                KeyValueCell(t, $"Assessed Value ({td.AssessmentLevelPercent:N0}%)", $"PHP {td.AssessedValue:N2}");
                KeyValueCell(t, "Annual RPT Due", $"PHP {td.AnnualTaxDue:N2}");
            });

            // 1. Owner, Taxpayer & Administrator / Beneficial User / Possessor
            col.Item().Element(c => SectionBox(c, "1. OWNER, TAXPAYER & ADMINISTRATOR / BENEFICIAL USER DETAILS", inner =>
            {
                inner.Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.3f);
                    });

                    KeyValueCell(t, "Declared Owner Name", ValueOr(td.OwnerName, "N/A"));
                    KeyValueCell(t, "Owner TIN", ValueOr(td.OwnerTin, "N/A"));
                    KeyValueCell(t, "Sex", ValueOr(td.OwnerSex, "N/A"));
                    KeyValueCell(t, "Civil Status", ValueOr(td.OwnerCivilStatus, "N/A"));
                    KeyValueCell(t, "Contact Phone", ValueOr(td.OwnerPhone, "N/A"));

                    t.Cell().ColumnSpan(3).BorderTop(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3.5f).Column(cc =>
                    {
                        cc.Item().Text("Owner Mailing Address").FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken2);
                        cc.Item().Text(ValueOr(td.OwnerAddress, "N/A")).FontSize(8);
                    });
                    t.Cell().ColumnSpan(2).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3.5f).Column(cc =>
                    {
                        cc.Item().Text("Owner Email Address").FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken2);
                        cc.Item().Text(ValueOr(td.OwnerEmail, "N/A")).FontSize(8);
                    });

                    var roles = new List<string>();
                    if (td.IsAdministrator) roles.Add("Administrator");
                    if (td.IsBeneficialUser) roles.Add("Beneficial User");
                    if (td.IsPossessor) roles.Add("Possessor");
                    string roleText = roles.Count > 0 ? string.Join(", ", roles) : "None";

                    KeyValueCell(t, "Admin / Possessor / Beneficial User", ValueOr(td.AdministratorName, "N/A"), topBorder: true);
                    KeyValueCell(t, "Role Designation", roleText, topBorder: true);
                    KeyValueCell(t, "Admin TIN", ValueOr(td.AdministratorTin, "N/A"), topBorder: true);
                    KeyValueCell(t, "Admin Gender", ValueOr(td.AdministratorGender, "N/A"), topBorder: true);
                    KeyValueCell(t, "Admin Contact No.", ValueOr(td.AdministratorContactNo, "N/A"), topBorder: true);

                    t.Cell().ColumnSpan(5).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3.5f).Column(cc =>
                    {
                        cc.Item().Text("Admin / Possessor Address").FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken2);
                        cc.Item().Text(ValueOr(td.AdministratorAddress, "N/A")).FontSize(8);
                    });
                });
            }));

            // 2. Property Location
            col.Item().Element(c => SectionBox(c, "2. PROPERTY LOCATION", inner =>
            {
                inner.Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.3f);
                        c.RelativeColumn(1.3f);
                        c.RelativeColumn(1.3f);
                    });

                    KeyValueCell(t, "No. / Street", ValueOr(td.StreetNo, "N/A"));
                    KeyValueCell(t, "Purok / Zone", ValueOr(td.Purok, "N/A"));
                    KeyValueCell(t, "Barangay / District", ValueOr(td.DistrictOrBarangay, "N/A"));
                    KeyValueCell(t, "City / Municipality", ValueOr(td.Municipality, "General Santos City"));
                    KeyValueCell(t, "Province / City", ValueOr(td.ProvinceCity, "South Cotabato"));
                });
            }));

            // 3. Kind-Specific Fields & Tables
            if (td.Kind == PropertyKind.Land)
            {
                ComposeLandRecordSections(col, td);
            }
            else if (td.Kind == PropertyKind.Building)
            {
                ComposeBuildingRecordSections(col, td);
            }
            else if (td.Kind == PropertyKind.Machinery)
            {
                ComposeMachineryRecordSections(col, td);
            }

            // 4. Valuation, Taxability, Effectivity & Annual Tax Summary
            col.Item().Element(c => SectionBox(c, "VALUATION, EFFECTIVITY, TAXABILITY & ANNUAL RPT SUMMARY", inner =>
            {
                inner.Column(vCol =>
                {
                    vCol.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        KeyValueCell(t, "Classification", td.Classification.ToString());
                        KeyValueCell(t, "Sub-Classification", ValueOr(td.SubClassification, "N/A"));
                        KeyValueCell(t, "Actual Use", ValueOr(td.ActualUse, td.Classification.ToString()));
                        KeyValueCell(t, "Area (sq.m.)", $"{td.AreaSqMeters:N2} sq.m.");
                        KeyValueCell(t, "Base Unit Value", $"PHP {td.UnitValue:N2}");
                        KeyValueCell(t, "Market Value (FMV)", $"PHP {td.MarketValue:N2}");

                        KeyValueCell(t, "Adjusted Market Value", $"PHP {td.AdjustedMarketValue:N2}", topBorder: true);
                        KeyValueCell(t, "Assessment Level", $"{td.AssessmentLevelPercent:N2}%", topBorder: true);
                        KeyValueCell(t, "Assessed Value (AV)", $"PHP {td.AssessedValue:N2}", topBorder: true);
                        KeyValueCell(t, "Taxability", ValueOr(td.Taxability, "Taxable"), topBorder: true);
                        KeyValueCell(t, "Effectivity", $"{ValueOr(td.EffectivityQuarter, "1st Qtr")} {td.EffectivityYear}", topBorder: true);
                        KeyValueCell(t, "Legal Basis", ValueOr(td.TaxabilityBasis, "RA 7160"), topBorder: true);

                        var basicRpt = Math.Round(td.AssessedValue * (td.BasicTaxRate / 100m), 2);
                        var sefRpt = Math.Round(td.AssessedValue * (td.SefTaxRate / 100m), 2);
                        var backTaxes = td.LandDetails?.BackTaxesYearFrom ?? td.BuildingDetails?.BackTaxesYearFrom ?? td.MachineryDetails?.BackTaxesYearFrom;
                        var backTaxesTo = td.LandDetails?.BackTaxesYearTo ?? td.BuildingDetails?.BackTaxesYearTo ?? td.MachineryDetails?.BackTaxesYearTo;

                        KeyValueCell(t, $"Basic RPT ({td.BasicTaxRate:N1}%)", $"PHP {basicRpt:N2}", topBorder: true);
                        KeyValueCell(t, $"SEF Tax ({td.SefTaxRate:N1}%)", $"PHP {sefRpt:N2}", topBorder: true);
                        KeyValueCell(t, "Total Annual Tax Due", $"PHP {td.AnnualTaxDue:N2}", topBorder: true);
                        KeyValueCell(t, "Tax Period", $"{td.TaxPeriodFromYear} - {td.TaxPeriodToYear}", topBorder: true);
                        KeyValueCell(t, "Back Taxes Period", backTaxes.HasValue ? $"{backTaxes} - {backTaxesTo}" : "None", topBorder: true);
                        KeyValueCell(t, "Approved Date", (td.DateApproved ?? td.DateAssessed).ToString("MMM dd, yyyy"), topBorder: true);
                    });

                    vCol.Item().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(txt =>
                    {
                        txt.Span("Total Assessed Value (Amount in Words): ").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                        txt.Span(AmountToWords(td.AssessedValue)).FontSize(7.5f).Bold().Italic();
                    });
                });
            }));

            // 5. Previous / Superseded Assessment Reference
            col.Item().Element(c => SectionBox(c, "SUPERSEDED / PREVIOUS ASSESSMENT RECORD REFERENCE", inner =>
            {
                inner.Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    KeyValueCell(t, "Previous TD No.", ValueOr(td.PreviousTdNo, "None / New"));
                    KeyValueCell(t, "Previous PIN", ValueOr(td.PreviousPin, "N/A"));
                    KeyValueCell(t, "Previous Owner", ValueOr(td.PreviousOwner, "N/A"));
                    KeyValueCell(t, "Previous Market Value", $"PHP {td.PreviousMarketValue:N2}");
                    KeyValueCell(t, "Previous Assessed Value", $"PHP {td.PreviousAssessedValue:N2}");
                });
            }));

            // 6. Memoranda & Signatories / Audit Trail
            col.Item().Element(c => SectionBox(c, "MEMORANDA, OFFICIAL SIGNATORIES & AUDIT TRAIL", inner =>
            {
                inner.Column(mCol =>
                {
                    mCol.Item().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Column(mc =>
                    {
                        mc.Item().Text("Memoranda / Special Notes:").FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken2);
                        mc.Item().Text(ValueOr(td.Memoranda, "Standard real property assessment encoded pursuant to City Tax Ordinance.")).FontSize(7.5f);
                    });

                    mCol.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        KeyValueCell(t, "Appraised By", $"{ValueOr(td.AppraisedByName, "N/A")} ({(td.DateAppraised ?? td.DateAssessed):MM/dd/yyyy})");
                        KeyValueCell(t, "Assessed By", $"{ValueOr(td.AssessedByName, "N/A")} ({td.DateAssessed:MM/dd/yyyy})");
                        KeyValueCell(t, "Recommending Approval", $"{ValueOr(td.RecommendingApprovalName, "N/A")} ({(td.DateRecommended ?? td.DateAssessed):MM/dd/yyyy})");
                        KeyValueCell(t, "Approved By (City Assessor)", $"{ValueOr(td.ApprovedByName, "N/A")} ({(td.DateApproved ?? td.DateAssessed):MM/dd/yyyy})");
                        KeyValueCell(t, "Entry in Roll / Encoded By", $"{ValueOr(td.EntryInRollName, td.EncodedByName)} ({(td.EntryInRollDate ?? td.DateEncoded ?? td.DateAssessed):MM/dd/yyyy})");
                    });
                });
            }));
        });
    }

    // -------------------------------------------------------------------------
    // LAND MASTER RECORD SECTIONS (ALL FIELDS + 4 TABLES)
    // -------------------------------------------------------------------------
    private static void ComposeLandRecordSections(ColumnDescriptor col, TaxDeclaration td)
    {
        // 3A. Land Registration, Title & Cadastral Details
        col.Item().Element(c => SectionBox(c, "3A. LAND REGISTRATION, TITLE & CADASTRAL IDENTIFICATION", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                KeyValueCell(t, "Registration Status", ValueOr(td.RegistrationStatus, "Titled"));
                KeyValueCell(t, "Title Type", ValueOr(td.TitleType, "OCT"));
                KeyValueCell(t, "Title No.", ValueOr(td.TitleNo, "N/A"));
                KeyValueCell(t, "Date Registered", td.DateRegistered?.ToString("MMM dd, yyyy") ?? "N/A");
                KeyValueCell(t, "Survey No.", ValueOr(td.SurveyNo, "N/A"));

                KeyValueCell(t, "Cadastral Survey No.", ValueOr(td.CadastralSurveyNo, "N/A"), topBorder: true);
                KeyValueCell(t, "Lot No.", ValueOr(td.LotNo, "N/A"), topBorder: true);
                KeyValueCell(t, "Block No.", ValueOr(td.BlockNo, "N/A"), topBorder: true);
                KeyValueCell(t, "Section No.", ValueOr(td.SectionNo, "N/A"), topBorder: true);
                KeyValueCell(t, "Parcel No.", ValueOr(td.ParcelNo, "N/A"), topBorder: true);
            });
        }));

        // 3B. Property Boundaries & Boundary Remarks Table
        col.Item().Element(c => SectionBox(c, "3B. PROPERTY BOUNDARIES & REMARKS", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(2.5f);
                    c.RelativeColumn(2.5f);
                });

                TableHeader(t, "Direction", "Boundary Description", "Remarks");

                TableRow(t, "NORTH (N)", ValueOr(td.BoundaryNorth, "N/A"), ValueOr(td.Nremarks, "-"));
                TableRow(t, "EAST (E)", ValueOr(td.BoundaryEast, "N/A"), ValueOr(td.Eremarks, "-"));
                TableRow(t, "SOUTH (S)", ValueOr(td.BoundarySouth, "N/A"), ValueOr(td.Sremarks, "-"));
                TableRow(t, "WEST (W)", ValueOr(td.BoundaryWest, "N/A"), ValueOr(td.Wremarks, "-"));
            });
        }));

        // Table 1: Land Appraisal Schedule
        var appraisalRows = GetLandAppraisalRows(td);
        col.Item().Element(c => SectionBox(c, "3C. LAND APPRAISAL SCHEDULE (CLASSIFICATION, AREA & BASE MARKET VALUE)", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(0.9f);
                });

                TableHeader(t, "Classification", "Sub-Class", "Actual Use", "Area (sq.m.)", "Area (has.)", "Unit Value", "Market Value", "Taxable");

                foreach (var r in appraisalRows)
                {
                    TableRow(t,
                        ValueOr(r.ClassificationName, "RESIDENTIAL"),
                        ValueOr(r.SubClassName, "1st Class"),
                        ValueOr(r.ActualUseName, "RESIDENTIAL"),
                        $"{r.AreaSQM:N2}",
                        $"{r.AreaHAS:N4}",
                        $"PHP {r.UnitValue:N2}",
                        $"PHP {r.MarketValue:N2}",
                        r.IsTaxable ? "Yes" : "No");
                }
            });
        }));

        // Table 2: Other Land Improvements Schedule
        var impRows = GetLandImprovementRows(td);
        col.Item().Element(c => SectionBox(c, "3D. OTHER LAND IMPROVEMENTS / TREES & PLANTS SCHEDULE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1.4f);
                });

                TableHeader(t, "Product / Improvement", "Area Planted", "Fruit Bearing", "Qty", "Age (Yrs)", "Unit Value", "Market Value", "Ass. Lvl", "Assessed Value");

                if (impRows.Count == 0)
                {
                    t.Cell().ColumnSpan(9).Padding(4).AlignCenter().Text("No additional land improvements encoded.").FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    foreach (var r in impRows)
                    {
                        TableRow(t,
                            ValueOr(r.ProductClass, "Improvement"),
                            r.AreaPlanted.HasValue ? $"{r.AreaPlanted.Value:N2}" : "-",
                            r.IsFruitBearing ? "Yes" : "No",
                            $"{r.Quantity:N2}",
                            r.AgeYears?.ToString() ?? "-",
                            $"PHP {r.UnitValue:N2}",
                            $"PHP {r.MarketValue:N2}",
                            $"{r.AssessmentLevel:N0}%",
                            $"PHP {r.AssessedValue:N2}");
                    }
                }
            });
        }));

        // Table 3: Market Value Adjustment Factors Schedule
        var adjRows = GetLandAdjustmentRows(td);
        col.Item().Element(c => SectionBox(c, "3E. MARKET VALUE ADJUSTMENT FACTORS SCHEDULE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(2f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.5f);
                });

                TableHeader(t, "Classification", "Base Market Value", "Adjustment Factor", "% Adj.", "Value Adjustment", "Adjusted Market Value");

                if (adjRows.Count == 0)
                {
                    TableRow(t,
                        td.Classification.ToString().ToUpperInvariant(),
                        $"PHP {td.MarketValue:N2}",
                        ValueOr(td.AdjustmentFactor, "None (100%)"),
                        $"{td.PercentAdjustment:N2}%",
                        $"PHP {td.ValueAdjustment:N2}",
                        $"PHP {td.AdjustedMarketValue:N2}");
                }
                else
                {
                    foreach (var r in adjRows)
                    {
                        TableRow(t,
                            ValueOr(r.ClassificationName, "RESIDENTIAL"),
                            $"PHP {r.BaseMarketValue:N2}",
                            ValueOr(r.AdjustmentFactor, "Standard"),
                            $"{r.PercentAdjustment:N2}%",
                            $"PHP {r.ValueAdjustment:N2}",
                            $"PHP {r.AdjustedMarketValue:N2}");
                    }
                }
            });
        }));

        // Table 4: Land Property Assessment Summary Schedule
        var assRows = BuildAssessmentRows(td);
        col.Item().Element(c => SectionBox(c, "3F. LAND PROPERTY ASSESSMENT ROLL SCHEDULE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.8f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.8f);
                    c.RelativeColumn(1.8f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.8f);
                });

                TableHeader(t, "Kind / Classification", "Area (sq.m.)", "Market Value", "Actual Use", "Assessment Level", "Assessed Value");

                foreach (var r in assRows)
                {
                    TableRow(t,
                        r.Classification,
                        $"{r.AreaSqm:N2}",
                        $"PHP {r.MarketValue:N2}",
                        r.ActualUse,
                        $"{r.AssessmentLevel:N0}%",
                        $"PHP {r.AssessedValue:N2}");
                }
            });
        }));
    }

    // -------------------------------------------------------------------------
    // BUILDING MASTER RECORD SECTIONS (ALL FIELDS + 3 TABLES)
    // -------------------------------------------------------------------------
    private static void ComposeBuildingRecordSections(ColumnDescriptor col, TaxDeclaration td)
    {
        var bldg = td.BuildingDetails;

        // 3A. Land Reference & Condominium (CCT) Details
        col.Item().Element(c => SectionBox(c, "3A. LAND REFERENCE (WHERE BUILDING IS ERECTED) & CONDOMINIUM (CCT) DETAILS", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.2f);
                });

                KeyValueCell(t, "Landowner Name", ValueOr(td.LandOwnerName, td.OwnerName));
                KeyValueCell(t, "Landowner TIN", ValueOr(td.LandOwnerTin, "N/A"));
                KeyValueCell(t, "Landowner Phone", ValueOr(td.LandOwnerPhone, "N/A"));
                KeyValueCell(t, "Land TD No.", ValueOr(td.LandTdNo, "N/A"));
                KeyValueCell(t, "Land PIN", ValueOr(td.LandPin, "N/A"));

                KeyValueCell(t, "Land Title No.", ValueOr(td.LandTitleNo, "N/A"), topBorder: true);
                KeyValueCell(t, "Land Survey No.", ValueOr(td.LandSurveyNo, "N/A"), topBorder: true);
                KeyValueCell(t, "Land Lot / Block No.", $"{ValueOr(td.LandLotNo, "-")} / {ValueOr(td.LandBlockNo, "-")}", topBorder: true);
                KeyValueCell(t, "Land Area (sq.m.)", $"{td.LandAreaSqm:N2} sq.m.", topBorder: true);
                KeyValueCell(t, "Is Condominium (CCT)", td.IsCct ? "Yes (CCT)" : "No", topBorder: true);

                KeyValueCell(t, "CCT Title No.", ValueOr(td.CctTitleNo, "N/A"), topBorder: true);
                KeyValueCell(t, "CCT Date Registered", td.CctDateRegistered?.ToString("MMM dd, yyyy") ?? "N/A", topBorder: true);
                t.Cell().ColumnSpan(3).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3.5f).Column(cc =>
                {
                    cc.Item().Text("Owner Reflected in CCT / Landowner Address").FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken2);
                    cc.Item().Text($"{ValueOr(td.OwnerNameReflectedInCct, "-")} | {ValueOr(td.LandOwnerAddress, "-")}").FontSize(8);
                });
            });
        }));

        // 3B. Structural Characteristics, Permits, Timeline & Depreciation Summary
        col.Item().Element(c => SectionBox(c, "3B. BUILDING STRUCTURAL CHARACTERISTICS, PERMITS & DEPRECIATION SUMMARY", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                KeyValueCell(t, "Structural Type", ValueOr(td.StructuralType, "Type I"));
                KeyValueCell(t, "Building Classification", ValueOr(td.BuildingClassification, "N/A"));
                KeyValueCell(t, "Building Use Type", ValueOr(td.BuildingUseType, "N/A"));
                KeyValueCell(t, "No. of Storeys / Floor Level", $"{td.NoOfStoreys} ({ValueOr(td.FloorLevel, "Ground")})");
                KeyValueCell(t, "Total Floor Area", $"{td.TotalBuildingAreaSqm:N2} sq.m.");

                KeyValueCell(t, "Building Permit No.", ValueOr(td.BuildingPermitNo, "N/A"), topBorder: true);
                KeyValueCell(t, "Occupancy Permit No.", ValueOr(td.OccupancyPermitNo, "N/A"), topBorder: true);
                KeyValueCell(t, "Date Constructed", td.DateConstructed?.ToString("MMM dd, yyyy") ?? "N/A", topBorder: true);
                KeyValueCell(t, "Date Completed", td.DateCompleted?.ToString("MMM dd, yyyy") ?? "N/A", topBorder: true);
                KeyValueCell(t, "Date Occupied / Age", $"{(td.DateOccupied?.ToString("MMM dd, yyyy") ?? "N/A")} ({td.BuildingAgeYears} yrs)", topBorder: true);

                var baseRcn = bldg?.BaseRcnValue ?? 0m;
                var rcn = bldg?.ReplacementCostNew ?? baseRcn;
                var deprPct = bldg?.BuildingDepreciationPercent ?? 0m;
                var deprVal = bldg?.BuildingDepreciationAccumulated ?? 0m;
                var deprMv = bldg?.DepreciatedMarketValue ?? td.MarketValue;
                var addlVal = bldg?.TotalAdditionalItemsValue ?? 0m;

                KeyValueCell(t, "Base RCN / RCN", $"PHP {baseRcn:N2} / PHP {rcn:N2}", topBorder: true);
                KeyValueCell(t, $"Depreciation ({deprPct:N1}%)", $"PHP {deprVal:N2}", topBorder: true);
                KeyValueCell(t, "Depreciated Core MV", $"PHP {deprMv:N2}", topBorder: true);
                KeyValueCell(t, "Additional Items Total", $"PHP {addlVal:N2}", topBorder: true);
                KeyValueCell(t, "Combined Building FMV", $"PHP {td.MarketValue:N2}", topBorder: true);
            });
        }));

        // Table 1: Building Floor-by-Floor Schedule
        var floors = GetBuildingFloorRows(td);
        col.Item().Element(c => SectionBox(c, "3C. BUILDING FLOOR-BY-FLOOR AREA & VALUATION SCHEDULE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(2f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.6f);
                });

                TableHeader(t, "Floor Number", "Description", "Floor Area (sq.m.)", "Base Unit Cost", "Market Value");

                foreach (var f in floors)
                {
                    TableRow(t,
                        ValueOr(f.FloorNumber, "1st Floor"),
                        ValueOr(f.Description, "Ground Level"),
                        $"{f.FloorAreaSqm:N2}",
                        $"PHP {f.BaseUnitCost:N2}",
                        $"PHP {f.MarketValue:N2}");
                }
            });
        }));

        // Table 2: Structural Materials & Finishes Checklist
        var materials = GetBuildingMaterialRows(td);
        col.Item().Element(c => SectionBox(c, "3D. BUILDING STRUCTURAL MATERIALS & FINISHES SCHEDULE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(2f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(2f);
                });

                TableHeader(t, "Structural Component", "Material / Finish", "Structural Component", "Material / Finish");

                for (int i = 0; i < materials.Count; i += 2)
                {
                    var left = materials[i];
                    (string Part, string Material) right = (i + 1 < materials.Count) ? materials[i + 1] : ("", "");
                    TableRow(t, left.Part, left.Material, right.Part, right.Material);
                }
            });
        }));

        // Table 3: Building Additional Items Schedule
        var additions = GetBuildingAdditionalRows(td);
        col.Item().Element(c => SectionBox(c, "3E. BUILDING ADDITIONAL ITEMS & IMPROVEMENTS SCHEDULE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.5f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1.6f);
                });

                TableHeader(t, "Item Description", "Rate Type", "Quantity", "Unit Rate", "Multiplier %", "Total Value");

                if (additions.Count == 0)
                {
                    t.Cell().ColumnSpan(6).Padding(4).AlignCenter().Text("No additional building items encoded.").FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    foreach (var a in additions)
                    {
                        TableRow(t,
                            ValueOr(a.ItemDescription, "Additional Item"),
                            ValueOr(a.RateType, "FixedCost"),
                            $"{a.Quantity:N2}",
                            $"PHP {a.UnitRate:N2}",
                            $"{a.MultiplierPercent:N1}%",
                            $"PHP {a.TotalValue:N2}");
                    }
                }
            });
        }));
    }

    // =========================================================================
    // ANNEX I-F: FIELD APPRAISAL AND ASSESSMENT SHEET – BUILDING AND OTHER STRUCTURES
    // Exact Sequence Matching Local Assessment Manual (Pages 1 to 4)
    // =========================================================================
    private static void ComposeBuildingAnnexIfHeader(IContainer container, TaxDeclaration td)
    {
        container.PaddingBottom(4).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("");
                r.AutoItem().Text("Local Assessment Manual").FontSize(7f).FontColor(Colors.Grey.Darken2);
            });

            col.Item().PaddingTop(1).AlignCenter()
                .Text("Annex I-F. Field Appraisal and Assessment Sheet - Building and Other Structures")
                .FontSize(10f).Bold().FontColor(Colors.Black);

            if (td.IsCancelledAndSuperseded || td.Status == AssessmentStatus.Cancelled)
            {
                col.Item().PaddingTop(3).Border(1).BorderColor(Colors.Red.Medium).Background(Colors.Red.Lighten5).Padding(4).Row(r =>
                {
                    var reason = string.IsNullOrWhiteSpace(td.CancellationReason) ? "" : $" due to {td.CancellationReason}";
                    var dateStr = (td.DateCancelled ?? td.DateAssessed).ToString("MMMM dd, yyyy");
                    var supStr = string.IsNullOrWhiteSpace(td.SupersededByTdns) ? "" : $" | Superseded by TD(s): {td.SupersededByTdns}";
                    r.RelativeItem().AlignCenter().Text($"*** CANCELLED & SUPERSEDED on {dateStr}{reason}{supStr} ***").FontSize(7.5f).Bold().FontColor(Colors.Red.Darken3);
                });
            }
        });
    }

    private static string CheckBoxText(bool isChecked, string label) =>
        isChecked ? $"[X] {label}" : $"[   ] {label}";

    private static void UnderlinedField(IContainer container, string label, string? value, float labelFontSize = 7.5f)
    {
        container.Row(r =>
        {
            r.AutoItem().PaddingRight(4).Text(label).FontSize(labelFontSize).FontColor(Colors.Black);
            r.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2).PaddingBottom(1)
                .Text(string.IsNullOrWhiteSpace(value) ? " " : value.Trim()).FontSize(labelFontSize).Bold().FontColor(Colors.Black);
        });
    }

    private static void ComposeBuildingAnnexIfContent(IContainer container, TaxDeclaration td)
    {
        var bldg = td.BuildingDetails;
        var floors = GetBuildingFloorRows(td);
        var materials = GetBuildingMaterialRows(td);
        var additions = GetBuildingAdditionalRows(td);

        container.Column(col =>
        {
            col.Spacing(5);

            // Data Privacy Notice
            col.Item().Text(txt =>
            {
                txt.Span("Data Privacy Notice: ").FontSize(7f).Italic().FontColor(Colors.Grey.Darken3);
                txt.Span("By submitting/providing your property information (including but not limited to name, contact details, property address, title number, and property documents), you acknowledge and authorize the government to collect, process, and store your personal data.")
                    .FontSize(7f).FontColor(Colors.Grey.Darken3);
            });

            // -----------------------------------------------------------------
            // 1. TRANSACTION INFORMATION (Page 1)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("TRANSACTION INFORMATION").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(tc =>
            {
                tc.Spacing(4);
                tc.Item().Row(r =>
                {
                    r.RelativeItem(3).Element(e => UnderlinedField(e, "Tax Declaration (TD) No.:", td.TaxDeclarationNumber));
                    r.ConstantItem(14);
                    r.RelativeItem(2).Element(e => UnderlinedField(e, "Transaction Code:", ValueOr(td.TransactionCodeDisplay, td.TransactionCode)));
                });
                tc.Item().Row(r =>
                {
                    r.RelativeItem(3).Element(e => UnderlinedField(e, "Property Identification No. (PIN):", td.PropertyIndexNumber));
                    r.RelativeItem(2).Text("");
                });
            });

            // -----------------------------------------------------------------
            // 2. PROPERTY REGISTRATION DETAILS (Page 1)
            // -----------------------------------------------------------------
            bool isCct = td.IsCct || string.Equals(td.TitleType, "CCT", StringComparison.OrdinalIgnoreCase);
            bool isTct = !isCct && (string.Equals(td.TitleType, "TCT", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(td.TitleNo) || !string.IsNullOrWhiteSpace(td.LandTitleNo));
            string regTitleNo = isCct ? ValueOr(td.CctTitleNo, td.TitleNo ?? "") : ValueOr(td.TitleNo, td.LandTitleNo ?? "");
            string regDateStr = (isCct ? (td.CctDateRegistered ?? td.DateRegistered) : td.DateRegistered)?.ToString("MMM dd, yyyy") ?? "";

            col.Item().PaddingTop(2).Text("PROPERTY REGISTRATION DETAILS").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(0.9f);
                });

                t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(lc =>
                {
                    lc.Spacing(4);
                    lc.Item().Row(r =>
                    {
                        r.AutoItem().PaddingRight(8).Text("Title Type:").FontSize(7.5f);
                        r.AutoItem().PaddingRight(16).Text(CheckBoxText(isCct, "CCT")).FontSize(7.5f).Bold();
                        r.AutoItem().Text(CheckBoxText(isTct, "TCT")).FontSize(7.5f).Bold();
                    });
                    lc.Item().Element(e => UnderlinedField(e, "Title No.:", regTitleNo));
                    lc.Item().Element(e => UnderlinedField(e, "Date Registered:", regDateStr));
                });

                t.Cell().Padding(5).Column(rc =>
                {
                    rc.Item().Text("");
                });
            });

            // -----------------------------------------------------------------
            // 3. OWNER INFORMATION (Page 1)
            // -----------------------------------------------------------------
            bool ownerMale = string.Equals(td.OwnerSex, "Male", StringComparison.OrdinalIgnoreCase);
            bool ownerFemale = string.Equals(td.OwnerSex, "Female", StringComparison.OrdinalIgnoreCase);
            string ownerOtherSex = (!ownerMale && !ownerFemale && !string.IsNullOrWhiteSpace(td.OwnerSex)) ? td.OwnerSex : "";

            col.Item().PaddingTop(2).Text("OWNER INFORMATION").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.35f);
                    c.RelativeColumn(1f);
                });

                t.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(lc =>
                {
                    lc.Spacing(4);
                    lc.Item().Text(txt =>
                    {
                        txt.Span("Owner/s Name ").FontSize(7.5f).Bold();
                        txt.Span("(as reflected in the CCT/OCT/TCT/CLOA):").FontSize(7f).Italic();
                    });
                    lc.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2).PaddingBottom(1)
                        .Text(ValueOr(td.OwnerNameReflectedInCct, ValueOr(td.OwnerName, " "))).FontSize(8f).Bold();

                    lc.Item().PaddingTop(2).Row(r =>
                    {
                        r.AutoItem().PaddingRight(8).Text("Sex:").FontSize(7.5f);
                        r.AutoItem().PaddingRight(12).Text(CheckBoxText(ownerMale, "Male")).FontSize(7.5f).Bold();
                        r.AutoItem().PaddingRight(12).Text(CheckBoxText(ownerFemale, "Female")).FontSize(7.5f).Bold();
                        r.RelativeItem().Element(e => UnderlinedField(e, "Others:", ownerOtherSex));
                    });
                });

                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(rc =>
                {
                    rc.Spacing(5);
                    rc.Item().Element(e => UnderlinedField(e, "TIN:", td.OwnerTin));
                    rc.Item().Element(e => UnderlinedField(e, "Phone/Mobile No.:", td.OwnerPhone));
                    rc.Item().Element(e => UnderlinedField(e, "Email Address:", td.OwnerEmail));
                });

                t.Cell().ColumnSpan(2).Padding(5).Element(e => UnderlinedField(e, "Mailing Address:", td.OwnerAddress));
            });

            // -----------------------------------------------------------------
            // 4. ADMINISTRATOR / POSSESSOR (Page 1)
            // -----------------------------------------------------------------
            bool hasAdminOrPossessor = td.IsAdministrator || td.IsPossessor || !string.IsNullOrWhiteSpace(td.AdministratorName);
            bool adminMale = hasAdminOrPossessor && string.Equals(td.AdministratorGender, "Male", StringComparison.OrdinalIgnoreCase);
            bool adminFemale = hasAdminOrPossessor && string.Equals(td.AdministratorGender, "Female", StringComparison.OrdinalIgnoreCase);

            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.35f);
                    c.RelativeColumn(1f);
                });

                t.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(lc =>
                {
                    lc.Spacing(4);
                    lc.Item().Row(r =>
                    {
                        r.AutoItem().PaddingRight(16).Text(CheckBoxText(td.IsAdministrator, "Administrator")).FontSize(7.5f).Bold();
                        r.AutoItem().Text(CheckBoxText(td.IsPossessor, "Possessor")).FontSize(7.5f).Bold();
                    });
                    lc.Item().Element(e => UnderlinedField(e, "Name/s:", td.AdministratorName));
                    lc.Item().Row(r =>
                    {
                        r.AutoItem().PaddingRight(8).Text("Sex:").FontSize(7.5f);
                        r.AutoItem().PaddingRight(14).Text(CheckBoxText(adminMale, "Male")).FontSize(7.5f).Bold();
                        r.AutoItem().Text(CheckBoxText(adminFemale, "Female")).FontSize(7.5f).Bold();
                    });
                });

                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(rc =>
                {
                    rc.Spacing(5);
                    rc.Item().Element(e => UnderlinedField(e, "TIN:", td.AdministratorTin));
                    rc.Item().Element(e => UnderlinedField(e, "Phone/Mobile No.:", td.AdministratorContactNo));
                    rc.Item().Element(e => UnderlinedField(e, "Email Address:", td.AdministratorEmail));
                });

                t.Cell().ColumnSpan(2).Padding(5).Element(e => UnderlinedField(e, "Mailing Address:", td.AdministratorAddress));
            });

            // -----------------------------------------------------------------
            // 5. PROPERTY TYPE ACCORDING TO USE (Page 2)
            // -----------------------------------------------------------------
            string useType = (td.BuildingUseType ?? "").Trim();
            bool isTownhouse = useType.Contains("Townhouse", StringComparison.OrdinalIgnoreCase);
            bool isResCondo = useType.Contains("Residential Condominium", StringComparison.OrdinalIgnoreCase);
            bool isComCondo = useType.Contains("Commercial Condominium", StringComparison.OrdinalIgnoreCase);
            bool isIndCondo = useType.Contains("Industrial Condominium", StringComparison.OrdinalIgnoreCase);
            bool isOtherUse = !isTownhouse && !isResCondo && !isComCondo && !isIndCondo && !string.IsNullOrWhiteSpace(useType);
            string otherUseText = isOtherUse ? useType : "";

            col.Item().PaddingTop(2).Text("PROPERTY TYPE ACCORDING TO USE").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(uc =>
            {
                uc.Spacing(4);
                uc.Item().Row(r =>
                {
                    r.RelativeItem(1).Text(CheckBoxText(isTownhouse, "Townhouse")).FontSize(7.5f).Bold();
                    r.RelativeItem(1.2f).Text(CheckBoxText(isResCondo, "Residential Condominium")).FontSize(7.5f).Bold();
                    r.RelativeItem(1.2f).Text(CheckBoxText(isComCondo, "Commercial Condominium")).FontSize(7.5f).Bold();
                });
                uc.Item().Row(r =>
                {
                    r.RelativeItem(1).Text(CheckBoxText(isIndCondo, "Industrial Condominium")).FontSize(7.5f).Bold();
                    r.RelativeItem(2.4f).Element(e => UnderlinedField(e, CheckBoxText(isOtherUse, "Other Type of Buildings and/or Structure:"), otherUseText));
                });
            });

            // -----------------------------------------------------------------
            // 6. PROPERTY LOCATION (Page 2)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("PROPERTY LOCATION").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(loc =>
            {
                loc.Spacing(4);
                loc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "No./Street:", td.StreetNo));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "Barangay/District:", td.DistrictOrBarangay));
                });
                loc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "Municipality:", ValueOr(td.Municipality, "General Santos City")));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "Province/City:", ValueOr(td.ProvinceCity, "South Cotabato")));
                });
            });

            // -----------------------------------------------------------------
            // 7. LAND REFERENCE (Page 2)
            // -----------------------------------------------------------------
            string landOwnerSex = ValueOr(td.LandOwnerSex, td.OwnerSex);
            bool landOwnerMale = string.Equals(landOwnerSex, "Male", StringComparison.OrdinalIgnoreCase);
            bool landOwnerFemale = string.Equals(landOwnerSex, "Female", StringComparison.OrdinalIgnoreCase);

            col.Item().PaddingTop(2).Text("LAND REFERENCE").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.35f);
                    c.RelativeColumn(1f);
                });

                t.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(lc =>
                {
                    lc.Spacing(4);
                    lc.Item().Element(e => UnderlinedField(e, "Owner/s Name:", ValueOr(td.LandOwnerName, td.OwnerName)));
                    lc.Item().Row(r =>
                    {
                        r.AutoItem().PaddingRight(8).Text("Sex:").FontSize(7.5f);
                        r.AutoItem().PaddingRight(14).Text(CheckBoxText(landOwnerMale, "Male")).FontSize(7.5f).Bold();
                        r.AutoItem().Text(CheckBoxText(landOwnerFemale, "Female")).FontSize(7.5f).Bold();
                    });
                });

                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(rc =>
                {
                    rc.Spacing(4);
                    rc.Item().Element(e => UnderlinedField(e, "TIN:", ValueOr(td.LandOwnerTin, td.OwnerTin)));
                    rc.Item().Element(e => UnderlinedField(e, "Phone/Mobile No.:", ValueOr(td.LandOwnerPhone, td.OwnerPhone)));
                    rc.Item().Element(e => UnderlinedField(e, "Email Address:", ValueOr(td.LandOwnerEmail, td.OwnerEmail)));
                });

                t.Cell().ColumnSpan(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5)
                    .Element(e => UnderlinedField(e, "Mailing Address:", ValueOr(td.LandOwnerAddress, td.OwnerAddress)));

                t.Cell().ColumnSpan(2).Padding(5).Column(bc =>
                {
                    bc.Spacing(4);
                    bc.Item().Row(r =>
                    {
                        r.RelativeItem().Element(e => UnderlinedField(e, "TD No.:", td.LandTdNo));
                        r.ConstantItem(14);
                        r.RelativeItem().Element(e => UnderlinedField(e, "PIN:", td.LandPin));
                    });
                    bc.Item().Row(r =>
                    {
                        r.RelativeItem(1.2f).Element(e => UnderlinedField(e, "Survey No.:", td.LandSurveyNo));
                        r.ConstantItem(10);
                        r.RelativeItem(0.9f).Element(e => UnderlinedField(e, "Lot No.:", td.LandLotNo));
                        r.ConstantItem(10);
                        r.RelativeItem(0.9f).Element(e => UnderlinedField(e, "Block No.:", td.LandBlockNo));
                    });
                    bc.Item().Row(r =>
                    {
                        r.RelativeItem().Element(e => UnderlinedField(e, "Title No.:", td.LandTitleNo));
                        r.ConstantItem(14);
                        r.RelativeItem().Element(e => UnderlinedField(e, "Area (sq.m.):", td.LandAreaSqm > 0 ? $"{td.LandAreaSqm:N2}" : ""));
                    });
                });
            });

            // -----------------------------------------------------------------
            // 8. GENERAL INFORMATION (Page 2)
            // -----------------------------------------------------------------
            var normClass = BuildingDepreciationSchedule.NormalizeClassificationCode(td.BuildingClassification, td.StructuralType);
            string stRaw = (td.StructuralType ?? "").ToUpperInvariant();
            bool isTypeI = stRaw.Contains("TYPE I") && !stRaw.Contains("TYPE II") && !stRaw.Contains("TYPE III") && !stRaw.Contains("TYPE IV") || normClass == "I";
            bool isTypeII = stRaw.Contains("TYPE II") && !stRaw.Contains("TYPE III") || normClass.StartsWith("II-");
            bool isTypeIII = stRaw.Contains("TYPE III") || normClass.StartsWith("III-");
            bool isTypeIV = stRaw.Contains("TYPE IV") || normClass.StartsWith("IV-");
            bool isTypeV = stRaw.Contains("TYPE V") || normClass.StartsWith("V-");

            col.Item().PaddingTop(2).Text("GENERAL INFORMATION").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().PaddingBottom(2).Row(r =>
            {
                r.AutoItem().PaddingRight(8).Text("Structural Type:").FontSize(7.5f).Bold();
                r.AutoItem().PaddingRight(10).Text(CheckBoxText(isTypeI, "Type I")).FontSize(7.5f).Bold();
                r.AutoItem().PaddingRight(10).Text(CheckBoxText(isTypeII, "Type II")).FontSize(7.5f).Bold();
                r.AutoItem().PaddingRight(10).Text(CheckBoxText(isTypeIII, "Type III")).FontSize(7.5f).Bold();
                r.AutoItem().PaddingRight(10).Text(CheckBoxText(isTypeIV, "Type IV")).FontSize(7.5f).Bold();
                r.AutoItem().PaddingRight(16).Text(CheckBoxText(isTypeV, "Type V")).FontSize(7.5f).Bold();
                r.RelativeItem().Element(e => UnderlinedField(e, "No. of Storey/s:", td.NoOfStoreys > 0 ? td.NoOfStoreys.ToString() : "1"));
            });

            decimal GetFloorArea(int floorIdx)
            {
                string ordinal = floorIdx switch
                {
                    1 => "1st",
                    2 => "2nd",
                    3 => "3rd",
                    _ => $"{floorIdx}th"
                };
                var match = floors.FirstOrDefault(f =>
                    !string.IsNullOrWhiteSpace(f.FloorNumber) &&
                    f.FloorNumber.Contains(ordinal, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match.FloorAreaSqm;
                if (floorIdx - 1 < floors.Count) return floors[floorIdx - 1].FloorAreaSqm;
                return 0m;
            }

            decimal first8Sum = 0m;
            for (int fi = 1; fi <= 8; fi++) first8Sum += GetFloorArea(fi);
            decimal totalBldgArea = td.TotalBuildingAreaSqm > 0 ? td.TotalBuildingAreaSqm : floors.Sum(f => f.FloorAreaSqm);
            decimal otherFloorsSub = Math.Max(0m, totalBldgArea - first8Sum);

            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.25f);
                    c.RelativeColumn(1.35f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1f);
                });

                void LeftRightRow(string leftLabel, string leftVal, string floorLabel, decimal floorArea, bool boldRight = false)
                {
                    t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3)
                        .Text(leftLabel).FontSize(7.5f);
                    t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3)
                        .Text(leftVal).FontSize(7.5f).Bold();
                    t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3).AlignCenter()
                        .Text(floorLabel).FontSize(7.5f);
                    var areaTxt = floorArea > 0 ? $"{floorArea:N2}" : "";
                    var areaCell = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3).AlignRight();
                    if (boldRight) areaCell.Text(areaTxt).FontSize(7.5f).Bold();
                    else areaCell.Text(areaTxt).FontSize(7.5f);
                }

                // Row 1: Building Classification + Header (Floor Level | Area (sq.m.))
                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3)
                    .Text("Building Classification:").FontSize(7.5f);
                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3)
                    .Text($"Class {normClass} ({ValueOr(td.BuildingClassification, normClass)})").FontSize(7.5f).Bold();
                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter()
                    .Text("Floor Level").FontSize(7.5f).Bold();
                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter()
                    .Text("Area (sq.m.)").FontSize(7.5f).Bold();

                LeftRightRow("Building Permit No.:", ValueOr(td.BuildingPermitNo, ""), "1st Floor", GetFloorArea(1));
                LeftRightRow("Building Age:", $"{td.BuildingAgeYears} yr(s)", "2nd Floor", GetFloorArea(2));
                LeftRightRow("Date Completed:", td.DateCompleted?.ToString("MMM dd, yyyy") ?? "", "3rd Floor", GetFloorArea(3));
                LeftRightRow("Occupancy Permit No.:", ValueOr(td.OccupancyPermitNo, ""), "4th Floor", GetFloorArea(4));
                LeftRightRow("Date Occupied:", td.DateOccupied?.ToString("MMM dd, yyyy") ?? "", "5th Floor", GetFloorArea(5));

                // Left cell spans remaining 5 rows (6th, 7th, 8th, Other Floors Subtotal, Total Building Area)
                string otherStructSummary = additions.Count > 0
                    ? string.Join("; ", additions.Select(a => $"{a.ItemDescription} ({a.Quantity:N2} sq.m.)"))
                    : "";
                t.Cell().RowSpan(5).ColumnSpan(2).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(4).Column(oc =>
                {
                    oc.Item().Text(txt =>
                    {
                        txt.Span("Other Structure ").FontSize(7.5f);
                        txt.Span("(Indicate Structure and Floor area):").FontSize(7f).Italic();
                    });
                    if (!string.IsNullOrWhiteSpace(otherStructSummary))
                    {
                        oc.Item().PaddingTop(3).Text(otherStructSummary).FontSize(7.5f).Bold();
                    }
                });

                void RightOnlyRow(string floorLabel, string areaText, bool isBold = false)
                {
                    var lc = t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3).AlignCenter();
                    var rc = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3).AlignRight();
                    if (isBold)
                    {
                        lc.Text(floorLabel).FontSize(7.5f).Bold();
                        rc.Text(areaText).FontSize(8f).ExtraBold();
                    }
                    else
                    {
                        lc.Text(floorLabel).FontSize(7.5f);
                        rc.Text(areaText).FontSize(7.5f);
                    }
                }

                RightOnlyRow("6th Floor", GetFloorArea(6) > 0 ? $"{GetFloorArea(6):N2}" : "");
                RightOnlyRow("7th Floor", GetFloorArea(7) > 0 ? $"{GetFloorArea(7):N2}" : "");
                RightOnlyRow("8th Floor", GetFloorArea(8) > 0 ? $"{GetFloorArea(8):N2}" : "");

                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3).Column(sc =>
                {
                    sc.Item().Text("Other Floors Subtotal").FontSize(7.5f).Bold();
                    sc.Item().Text("(Attach additional sheet/s in case of additional floors)").FontSize(6f).Italic().FontColor(Colors.Grey.Darken2);
                });
                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(3).AlignMiddle().AlignRight()
                    .Text(otherFloorsSub > 0 ? $"{otherFloorsSub:N2}" : "").FontSize(7.5f);

                RightOnlyRow("Total Building Area", $"{totalBldgArea:N2} sq.m.", isBold: true);
            });

            // -----------------------------------------------------------------
            // 9. FLOOR PLAN BOX (Page 3)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(fp =>
            {
                fp.Item().Text(txt =>
                {
                    txt.Span("Floor Plan: ").FontSize(7.5f).Bold();
                    txt.Span("(Photograph of the Structure may also be attached)").FontSize(7f).Italic();
                });

                var baseRcn = bldg?.BaseRcnValue > 0 ? bldg.BaseRcnValue : floors.Sum(f => f.MarketValue);
                var deprPct = bldg?.BuildingDepreciationPercent ?? 0m;
                var deprMv = bldg?.DepreciatedMarketValue > 0 ? bldg.DepreciatedMarketValue : Math.Max(0m, baseRcn - (bldg?.BuildingDepreciationAccumulated ?? 0m));

                fp.Item().MinHeight(38).PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Column(c1 =>
                    {
                        c1.Item().Text("Apply Depreciation: Market Val = (Total Area x Unit Cost) x (1 - Depreciation Rate)").FontSize(7f).Italic().FontColor(Colors.Grey.Darken2);
                        c1.Item().Text($"Base RCN: PHP {baseRcn:N2}  |  Depreciation Rate: {deprPct:N1}%  |  Depreciated Core MV: PHP {deprMv:N2}").FontSize(7.5f).Bold();
                    });
                    if (!string.IsNullOrWhiteSpace(td.FloorPlanSketchPath))
                    {
                        r.AutoItem().Text($"Attached Sketch: {td.FloorPlanSketchPath}").FontSize(7f).FontColor(Colors.Teal.Darken3);
                    }
                });
            });

            // -----------------------------------------------------------------
            // 10. STRUCTURAL MATERIALS (Page 3 - Table 3 Codes)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.7f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.7f);
                });

                t.Cell().ColumnSpan(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3.5f)
                    .Text("STRUCTURAL MATERIALS").FontSize(8f).Bold();

                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter()
                    .Text("Structural Parts").FontSize(7.5f).Bold();
                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter()
                    .Text("Materials or Code").FontSize(7.5f).Bold();
                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter()
                    .Text("Structural Parts").FontSize(7.5f).Bold();
                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter()
                    .Text("Materials or Code").FontSize(7.5f).Bold();

                int half = (materials.Count + 1) / 2;
                for (int i = 0; i < half; i++)
                {
                    var left = materials[i];
                    (string Part, string Material) right = (i + half < materials.Count) ? materials[i + half] : ("", "");

                    t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(left.Part).FontSize(7.5f);
                    t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(left.Material).FontSize(7.5f).Bold();
                    t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(right.Part).FontSize(7.5f);
                    t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(right.Material).FontSize(7.5f).Bold();
                }
            });

            // -----------------------------------------------------------------
            // 11. BUILDING CORE (Page 3)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("BUILDING CORE").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.4f);
                });

                TableHeader(t, "Bldg. Classification", "Actual Use", "Total Area", "Unit Const. Cost (Php)", "Market Value (Php)");

                foreach (var f in floors)
                {
                    TableRow(t,
                        $"Class {normClass}",
                        ValueOr(td.BuildingUseType, ValueOr(td.ActualUse, "RESIDENTIAL")),
                        $"{f.FloorAreaSqm:N2} sq.m.",
                        $"PHP {f.BaseUnitCost:N2}",
                        $"PHP {f.MarketValue:N2}");
                }
            });

            // -----------------------------------------------------------------
            // 12. ADDITIONAL ITEM/S (Page 3)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("ADDITIONAL ITEM/S").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.4f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.5f);
                });

                TableHeader(t, "Particular Item", "Area", "Unit Const. Cost (Php)", "Market Value (Php)");

                if (additions.Count == 0)
                {
                    TableRow(t, "-", "-", "-", "PHP 0.00");
                }
                else
                {
                    foreach (var a in additions)
                    {
                        TableRow(t,
                            ValueOr(a.ItemDescription, "Improvement"),
                            $"{a.Quantity:N2}",
                            $"PHP {a.UnitRate:N2}",
                            $"PHP {a.TotalValue:N2}");
                    }
                }
            });

            // -----------------------------------------------------------------
            // 13. DEPRECIATION (Page 4)
            // -----------------------------------------------------------------
            var baseRcnVal = bldg?.BaseRcnValue > 0 ? bldg.BaseRcnValue : floors.Sum(f => f.MarketValue);
            var addlTotalVal = bldg?.TotalAdditionalItemsValue > 0 ? bldg.TotalAdditionalItemsValue : additions.Sum(a => a.TotalValue);
            var totalBeforeDepr = baseRcnVal + addlTotalVal;
            var deprRatePct = bldg?.BuildingDepreciationPercent ?? 0m;
            var deprValPhp = bldg?.BuildingDepreciationAccumulated ?? Math.Round(baseRcnVal * (deprRatePct / 100m), 2);
            var adjustedMvPhp = td.MarketValue;

            col.Item().PaddingTop(2).Text("DEPRECIATION").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.8f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.6f);
                });

                TableHeader(t,
                    "Total Market Value (Php)\n(Building + Additional Items)",
                    "Depreciation Rate (%)",
                    "Depreciated Value (Php)",
                    "Adjusted Market Value (Php)");

                TableRow(t,
                    $"PHP {totalBeforeDepr:N2}",
                    $"{deprRatePct:N1}%",
                    $"PHP {deprValPhp:N2}",
                    $"PHP {adjustedMvPhp:N2}");

                t.Cell().ColumnSpan(3).BorderTop(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3.5f).AlignRight()
                    .Text("Total: P").FontSize(8f).Bold();
                t.Cell().BorderTop(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3.5f).AlignRight()
                    .Text($"PHP {adjustedMvPhp:N2}").FontSize(8.5f).ExtraBold();
            });

            // -----------------------------------------------------------------
            // 14. PROPERTY ASSESSMENT (Page 4)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("PROPERTY ASSESSMENT").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.2f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.6f);
                });

                TableHeader(t,
                    "Adjusted Market Value\n(Land + Improvement) (Php)",
                    "Assessment Level (%)",
                    "Assessed Value (Php)");

                TableRow(t,
                    $"PHP {adjustedMvPhp:N2}",
                    $"{td.AssessmentLevelPercent:N0}%",
                    $"PHP {td.AssessedValue:N2}");

                t.Cell().ColumnSpan(2).BorderTop(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3.5f).AlignRight()
                    .Text("Total P").FontSize(8f).Bold();
                t.Cell().BorderTop(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3.5f).AlignRight()
                    .Text($"PHP {td.AssessedValue:N2}").FontSize(8.5f).ExtraBold().FontColor(Colors.Teal.Darken3);
            });

            // -----------------------------------------------------------------
            // 15. EFFECTIVITY OF ASSESSMENT/REASSESSMENT (Page 4)
            // -----------------------------------------------------------------
            string qtr = (td.EffectivityQuarter ?? "1st").ToUpperInvariant();
            bool q1 = qtr.Contains("1");
            bool q2 = qtr.Contains("2");
            bool q3 = qtr.Contains("3");
            bool q4 = qtr.Contains("4");
            bool isTaxable = !string.Equals(td.Taxability, "Exempt", StringComparison.OrdinalIgnoreCase);
            var backFrom = td.BackTaxesYearFrom?.ToString() ?? "";
            var backTo = td.BackTaxesYearTo?.ToString() ?? "";

            col.Item().PaddingTop(2).Text("EFFECTIVITY OF ASSESSMENT/REASSESSMENT").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(ec =>
            {
                ec.Spacing(4);
                ec.Item().Row(r =>
                {
                    r.RelativeItem(1.2f).Element(e => UnderlinedField(e, "Year:", td.EffectivityYear.ToString()));
                    r.ConstantItem(16);
                    r.AutoItem().PaddingRight(10).Text("Quarter:").FontSize(7.5f);
                    r.AutoItem().PaddingRight(12).Text(CheckBoxText(q1, "1st")).FontSize(7.5f).Bold();
                    r.AutoItem().PaddingRight(12).Text(CheckBoxText(q2, "2nd")).FontSize(7.5f).Bold();
                    r.AutoItem().PaddingRight(12).Text(CheckBoxText(q3, "3rd")).FontSize(7.5f).Bold();
                    r.AutoItem().Text(CheckBoxText(q4, "4th")).FontSize(7.5f).Bold();
                });
                ec.Item().Row(r =>
                {
                    r.AutoItem().PaddingRight(10).Text("Taxability:").FontSize(7.5f);
                    r.AutoItem().PaddingRight(14).Text(CheckBoxText(isTaxable, "Taxable")).FontSize(7.5f).Bold();
                    r.AutoItem().PaddingRight(18).Text(CheckBoxText(!isTaxable, "Exempt")).FontSize(7.5f).Bold();
                    r.RelativeItem().Element(e => UnderlinedField(e, "Basis:", ValueOr(td.AssessmentBasis, td.TaxabilityBasis)));
                });
            });

            col.Item().PaddingHorizontal(4).Row(r =>
            {
                r.AutoItem().PaddingRight(8).Text("Period Covered by Back Taxes (if any):").FontSize(7.5f);
                r.RelativeItem().Element(e => UnderlinedField(e, "Year:", backFrom));
                r.ConstantItem(12);
                r.RelativeItem().Element(e => UnderlinedField(e, "to Year:", backTo));
            });

            // -----------------------------------------------------------------
            // 16. RECORD OF SUPERSEDED ASSESSMENT (Page 4)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("RECORD OF SUPERSEDED ASSESSMENT").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(sc =>
            {
                sc.Spacing(4);
                sc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "Previous TD No.:", ValueOr(td.PreviousTdNo, "None / New")));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "PIN:", ValueOr(td.PreviousPin, "")));
                });
                sc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "Market Value:", td.PreviousMarketValue > 0 ? $"PHP {td.PreviousMarketValue:N2}" : ""));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "Assessed Value:", td.PreviousAssessedValue > 0 ? $"PHP {td.PreviousAssessedValue:N2}" : ""));
                });
                sc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "Previous Owner:", ValueOr(td.PreviousOwner, "")));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "Effectivity of Assessment:", ""));
                });
            });

            // -----------------------------------------------------------------
            // 17. SIGNATORIES: APPRAISED BY, ASSESSED BY, RECOMMENDING APPROVAL BY (Page 4)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(6).Row(r =>
            {
                void SignatoryBlock(IContainer c, string heading, string? name, DateTime? date)
                {
                    c.Column(sc =>
                    {
                        sc.Item().Text(heading).FontSize(7.5f).Bold();
                        sc.Item().PaddingTop(14).Row(sr =>
                        {
                            sr.RelativeItem(2.2f).Column(nc =>
                            {
                                nc.Item().BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter()
                                    .Text(ValueOr(name, " ")).FontSize(7.5f).Bold();
                                nc.Item().AlignCenter().Text("Name and Position").FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken2);
                            });
                            sr.ConstantItem(6);
                            sr.RelativeItem(1f).Column(dc =>
                            {
                                dc.Item().BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter()
                                    .Text((date ?? td.DateAssessed).ToString("MM/dd/yyyy")).FontSize(7.5f);
                                dc.Item().AlignCenter().Text("Date").FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken2);
                            });
                        });
                    });
                }

                r.RelativeItem().Element(c => SignatoryBlock(c, "APPRAISED BY:", td.AppraisedByName, td.DateAppraised));
                r.ConstantItem(12);
                r.RelativeItem().Element(c => SignatoryBlock(c, "ASSESSED BY:", td.AssessedByName, td.DateAssessed));
                r.ConstantItem(12);
                r.RelativeItem().Element(c => SignatoryBlock(c, "RECOMMENDING APPROVAL BY:", td.RecommendingApprovalName, td.DateRecommended));
            });

            // -----------------------------------------------------------------
            // 18. MEMORANDA (Page 4)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(4).Column(mc =>
            {
                mc.Item().Text("MEMORANDA:").FontSize(8f).Bold();
                mc.Item().PaddingTop(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2).PaddingBottom(2)
                    .Text(ValueOr(td.Memoranda, " ")).FontSize(7.5f);
                mc.Item().Height(12).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2);
                mc.Item().Height(12).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2);
            });
        });
    }

    // =========================================================================
    // ANNEX I-E: FIELD APPRAISAL AND ASSESSMENT SHEET - LAND/OTHER IMPROVEMENTS
    // Exact Sequence Matching Local Assessment Manual (Pages 1 to 3)
    // =========================================================================
    private static void ComposeLandAnnexIeHeader(IContainer container, TaxDeclaration td)
    {
        container.PaddingBottom(4).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("");
                r.AutoItem().Text("Local Assessment Manual").FontSize(7f).FontColor(Colors.Grey.Darken2);
            });

            col.Item().PaddingTop(1).AlignCenter()
                .Text("Annex I-E. Field Appraisal and Assessment Sheet - Land/Other Improvements")
                .FontSize(10f).Bold().FontColor(Colors.Black);

            if (td.IsCancelledAndSuperseded || td.Status == AssessmentStatus.Cancelled)
            {
                col.Item().PaddingTop(3).Border(1).BorderColor(Colors.Red.Medium).Background(Colors.Red.Lighten5).Padding(4).Row(r =>
                {
                    var reason = string.IsNullOrWhiteSpace(td.CancellationReason) ? "" : $" due to {td.CancellationReason}";
                    var dateStr = (td.DateCancelled ?? td.DateAssessed).ToString("MMMM dd, yyyy");
                    var supStr = string.IsNullOrWhiteSpace(td.SupersededByTdns) ? "" : $" | Superseded by TD(s): {td.SupersededByTdns}";
                    r.RelativeItem().AlignCenter().Text($"*** CANCELLED & SUPERSEDED on {dateStr}{reason}{supStr} ***").FontSize(7.5f).Bold().FontColor(Colors.Red.Darken3);
                });
            }
        });
    }

    private static void ComposeLandAnnexIeContent(IContainer container, TaxDeclaration td)
    {
        var appraisalRows = GetLandAppraisalRows(td);
        var impRows = GetLandImprovementRows(td);
        var adjRows = GetLandAdjustmentRows(td);
        var assRows = BuildAssessmentRows(td);

        container.Column(col =>
        {
            col.Spacing(5);

            // Data Privacy Notice
            col.Item().Text(txt =>
            {
                txt.Span("Data Privacy Notice: ").FontSize(7f).Italic().FontColor(Colors.Grey.Darken3);
                txt.Span("By submitting/providing your property information (including but not limited to name, contact details, property address, title number, and property documents), you acknowledge and authorize the government to collect, process, and store your personal data.")
                    .FontSize(7f).FontColor(Colors.Grey.Darken3);
            });

            // -----------------------------------------------------------------
            // 1. TRANSACTION INFORMATION (Page 1)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("TRANSACTION INFORMATION").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(tc =>
            {
                tc.Spacing(4);
                tc.Item().Row(r =>
                {
                    r.RelativeItem(3).Element(e => UnderlinedField(e, "Tax Declaration (TD) No.:", td.TaxDeclarationNumber));
                    r.ConstantItem(14);
                    r.RelativeItem(2).Element(e => UnderlinedField(e, "Transaction Code:", ValueOr(td.TransactionCodeDisplay, td.TransactionCode)));
                });
                tc.Item().Row(r =>
                {
                    r.RelativeItem(3).Element(e => UnderlinedField(e, "Property Identification No. (PIN):", td.PropertyIndexNumber));
                    r.RelativeItem(2).Text("");
                });
            });

            // -----------------------------------------------------------------
            // 2. PROPERTY REGISTRATION DETAILS (Page 1)
            // -----------------------------------------------------------------
            string regStatus = ValueOr(td.RegistrationStatus, "Titled");
            bool isUntitled = string.Equals(regStatus, "Untitled", StringComparison.OrdinalIgnoreCase);
            bool isTitled = !isUntitled;

            string titleType = (td.TitleType ?? "OCT").Trim().ToUpperInvariant();
            bool isOct = isTitled && titleType == "OCT";
            bool isTct = isTitled && titleType == "TCT";
            bool isCloa = isTitled && titleType == "CLOA";

            col.Item().PaddingTop(2).Text("PROPERTY REGISTRATION DETAILS").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(rc =>
            {
                rc.Spacing(4);
                rc.Item().Row(r =>
                {
                    r.AutoItem().PaddingRight(12).Text("Registration Status:").FontSize(7.5f);
                    r.AutoItem().PaddingRight(20).Text(CheckBoxText(isTitled, "Titled")).FontSize(7.5f).Bold();
                    r.AutoItem().Text(CheckBoxText(isUntitled, "Untitled")).FontSize(7.5f).Bold();
                });
                rc.Item().Row(r =>
                {
                    r.AutoItem().PaddingRight(12).Text("Title Type:").FontSize(7.5f);
                    r.AutoItem().PaddingRight(20).Text(CheckBoxText(isOct, "OCT")).FontSize(7.5f).Bold();
                    r.AutoItem().PaddingRight(20).Text(CheckBoxText(isTct, "TCT")).FontSize(7.5f).Bold();
                    r.AutoItem().Text(CheckBoxText(isCloa, "CLOA")).FontSize(7.5f).Bold();
                });
                rc.Item().Row(r =>
                {
                    r.RelativeItem(1.3f).Element(e => UnderlinedField(e, "Title No.:", td.TitleNo));
                    r.ConstantItem(14);
                    r.RelativeItem(1f).Element(e => UnderlinedField(e, "Date Registered:", td.DateRegistered?.ToString("MMM dd, yyyy") ?? ""));
                });
                rc.Item().Row(r =>
                {
                    r.RelativeItem(1.3f).Element(e => UnderlinedField(e, "Survey No.:", ValueOr(td.SurveyNo, td.CadastralSurveyNo ?? "")));
                    r.ConstantItem(12);
                    r.RelativeItem(0.9f).Element(e => UnderlinedField(e, "Lot No.:", td.LotNo));
                    r.ConstantItem(12);
                    r.RelativeItem(0.9f).Element(e => UnderlinedField(e, "Block No.:", td.BlockNo));
                });
            });

            // -----------------------------------------------------------------
            // 3. OWNER INFORMATION (Page 1)
            // -----------------------------------------------------------------
            bool ownerMale = string.Equals(td.OwnerSex, "Male", StringComparison.OrdinalIgnoreCase);
            bool ownerFemale = string.Equals(td.OwnerSex, "Female", StringComparison.OrdinalIgnoreCase);
            string ownerOtherSex = (!ownerMale && !ownerFemale && !string.IsNullOrWhiteSpace(td.OwnerSex)) ? td.OwnerSex : "";

            col.Item().PaddingTop(2).Text("OWNER INFORMATION").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.35f);
                    c.RelativeColumn(1f);
                });

                t.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(lc =>
                {
                    lc.Spacing(4);
                    lc.Item().Text(txt =>
                    {
                        txt.Span("Owner/s Name ").FontSize(7.5f).Bold();
                        txt.Span("(as reflected in the OCT/TCT/CLOA):").FontSize(7f).Italic();
                    });
                    lc.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2).PaddingBottom(1)
                        .Text(ValueOr(td.OwnerName, " ")).FontSize(8f).Bold();

                    lc.Item().PaddingTop(2).Row(r =>
                    {
                        r.AutoItem().PaddingRight(8).Text("Sex:").FontSize(7.5f);
                        r.AutoItem().PaddingRight(12).Text(CheckBoxText(ownerMale, "Male")).FontSize(7.5f).Bold();
                        r.AutoItem().PaddingRight(12).Text(CheckBoxText(ownerFemale, "Female")).FontSize(7.5f).Bold();
                        r.RelativeItem().Element(e => UnderlinedField(e, "Others:", ownerOtherSex));
                    });
                });

                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(rc =>
                {
                    rc.Spacing(5);
                    rc.Item().Element(e => UnderlinedField(e, "TIN:", td.OwnerTin));
                    rc.Item().Element(e => UnderlinedField(e, "Phone/Mobile No.:", td.OwnerPhone));
                    rc.Item().Element(e => UnderlinedField(e, "Email Address:", td.OwnerEmail));
                });

                t.Cell().ColumnSpan(2).Padding(5).Element(e => UnderlinedField(e, "Mailing Address:", td.OwnerAddress));
            });

            // -----------------------------------------------------------------
            // 4. ADMINISTRATOR / BENEFICIAL USER / POSSESSOR (Page 1)
            // -----------------------------------------------------------------
            bool hasAdminOrPossessor = td.IsAdministrator || td.IsBeneficialUser || td.IsPossessor || !string.IsNullOrWhiteSpace(td.AdministratorName);
            bool adminMale = hasAdminOrPossessor && string.Equals(td.AdministratorGender, "Male", StringComparison.OrdinalIgnoreCase);
            bool adminFemale = hasAdminOrPossessor && string.Equals(td.AdministratorGender, "Female", StringComparison.OrdinalIgnoreCase);

            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.35f);
                    c.RelativeColumn(1f);
                });

                t.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(lc =>
                {
                    lc.Spacing(4);
                    lc.Item().Row(r =>
                    {
                        r.AutoItem().PaddingRight(12).Text(CheckBoxText(td.IsAdministrator, "Administrator")).FontSize(7.5f).Bold();
                        r.AutoItem().PaddingRight(12).Text(CheckBoxText(td.IsBeneficialUser, "Beneficial User")).FontSize(7.5f).Bold();
                        r.AutoItem().Text(CheckBoxText(td.IsPossessor, "Possessor")).FontSize(7.5f).Bold();
                    });
                    lc.Item().Element(e => UnderlinedField(e, "Name/s:", td.AdministratorName));
                    lc.Item().Row(r =>
                    {
                        r.AutoItem().PaddingRight(8).Text("Sex:").FontSize(7.5f);
                        r.AutoItem().PaddingRight(14).Text(CheckBoxText(adminMale, "Male")).FontSize(7.5f).Bold();
                        r.AutoItem().Text(CheckBoxText(adminFemale, "Female")).FontSize(7.5f).Bold();
                    });
                });

                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(rc =>
                {
                    rc.Spacing(5);
                    rc.Item().Element(e => UnderlinedField(e, "TIN:", td.AdministratorTin));
                    rc.Item().Element(e => UnderlinedField(e, "Phone/Mobile No.:", td.AdministratorContactNo));
                    rc.Item().Element(e => UnderlinedField(e, "Email Address:", td.AdministratorEmail));
                });

                t.Cell().ColumnSpan(2).Padding(5).Element(e => UnderlinedField(e, "Mailing Address:", td.AdministratorAddress));
            });

            // -----------------------------------------------------------------
            // 5. PROPERTY LOCATION (Page 2)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("PROPERTY LOCATION").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(loc =>
            {
                loc.Spacing(4);
                loc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "No./Street:", td.StreetNo));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "Barangay/District:", td.DistrictOrBarangay));
                });
                loc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "Municipality:", ValueOr(td.Municipality, "General Santos City")));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "Province/City:", ValueOr(td.ProvinceCity, "South Cotabato")));
                });
            });

            // -----------------------------------------------------------------
            // 6. PROPERTY BOUNDARIES & LAND SKETCH (Page 2)
            // -----------------------------------------------------------------
            string FormatBoundary(string? bound, string? rem) =>
                string.IsNullOrWhiteSpace(rem) ? (bound ?? "") : $"{bound} ({rem})";

            col.Item().PaddingTop(2).Text("PROPERTY BOUNDARIES").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.2f);
                });

                t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Darken1).Padding(5).Column(bc =>
                {
                    bc.Spacing(4);
                    bc.Item().Element(e => UnderlinedField(e, "North:", FormatBoundary(td.BoundaryNorth, td.Nremarks)));
                    bc.Item().Element(e => UnderlinedField(e, "East:", FormatBoundary(td.BoundaryEast, td.Eremarks)));
                    bc.Item().Element(e => UnderlinedField(e, "West:", FormatBoundary(td.BoundaryWest, td.Wremarks)));
                    bc.Item().Element(e => UnderlinedField(e, "South:", FormatBoundary(td.BoundarySouth, td.Sremarks)));
                });

                t.Cell().Padding(5).Column(sc =>
                {
                    sc.Item().Text(txt =>
                    {
                        txt.Span("LAND SKETCH: ").FontSize(7.5f).Bold();
                        txt.Span("(Not necessarily drawn to scale)").FontSize(7f).Italic();
                    });
                    if (!string.IsNullOrWhiteSpace(td.FloorPlanSketchPath))
                    {
                        sc.Item().PaddingTop(6).Text($"Attached Sketch: {td.FloorPlanSketchPath}").FontSize(7.5f).FontColor(Colors.Teal.Darken3);
                    }
                });
            });

            // -----------------------------------------------------------------
            // 7. LAND APPRAISAL (Page 2)
            // -----------------------------------------------------------------
            decimal totalLandMv = appraisalRows.Sum(r => r.MarketValue);
            if (totalLandMv <= 0) totalLandMv = td.MarketValue;

            col.Item().PaddingTop(2).Text("LAND APPRAISAL").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.4f);
                });

                TableHeader(t, "Classification", "Sub-\nclassification", "Area", "Unit Value (Php)", "Market Value (Php)");

                foreach (var r in appraisalRows)
                {
                    TableRow(t,
                        ValueOr(r.ClassificationName, "RESIDENTIAL"),
                        ValueOr(r.SubClassName, "1st Class"),
                        $"{r.AreaSQM:N2} sq.m.",
                        $"P {r.UnitValue:N2}",
                        $"P {r.MarketValue:N2}");
                }
            });
            col.Item().Row(r =>
            {
                r.RelativeItem();
                r.AutoItem().PaddingRight(6).Text("Total   P").FontSize(8f).Bold();
                r.ConstantItem(130).BorderBottom(0.75f).BorderColor(Colors.Black).AlignRight()
                    .Text($"{totalLandMv:N2}").FontSize(8.5f).ExtraBold();
            });

            // -----------------------------------------------------------------
            // 8. OTHER IMPROVEMENTS (Page 2)
            // -----------------------------------------------------------------
            decimal totalImpMv = impRows.Sum(r => r.MarketValue);

            col.Item().PaddingTop(2).Text("OTHER IMPROVEMENTS").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(0.9f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.4f);
                });

                TableHeader(t, "Type of Improvements", "Quantity", "Unit of Measure", "Unit Value (Php)", "Market Value (Php)");

                if (impRows.Count == 0)
                {
                    TableRow(t, "-", "-", "-", "P 0.00", "P 0.00");
                }
                else
                {
                    foreach (var r in impRows)
                    {
                        TableRow(t,
                            ValueOr(r.ProductClass, "Improvement"),
                            $"{r.Quantity:N2}",
                            ValueOr(td.LandDetails?.ImprovementUnitOfMeasure, "pcs / sq.m."),
                            $"P {r.UnitValue:N2}",
                            $"P {r.MarketValue:N2}");
                    }
                }
            });
            col.Item().Row(r =>
            {
                r.RelativeItem();
                r.AutoItem().PaddingRight(6).Text("Total   P").FontSize(8f).Bold();
                r.ConstantItem(130).BorderBottom(0.75f).BorderColor(Colors.Black).AlignRight()
                    .Text($"{totalImpMv:N2}").FontSize(8.5f).ExtraBold();
            });

            // -----------------------------------------------------------------
            // 9. MARKET VALUE ADJUSTMENT (Page 2)
            // -----------------------------------------------------------------
            decimal totalAdjMv = adjRows.Count > 0
                ? adjRows.Sum(r => r.AdjustedMarketValue)
                : (td.AdjustedMarketValue > 0 ? td.AdjustedMarketValue : (totalLandMv + totalImpMv));

            col.Item().PaddingTop(2).Text("MARKET VALUE ADJUSTMENT").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.5f);
                });

                TableHeader(t,
                    "Market Value\n(Land + Improvement) (Php)",
                    "Adjustment\nFactor",
                    "Percent (%)\nAdjustment",
                    "Value Adjustment\n(Php)",
                    "Adjusted Market Value\n(Php)");

                if (adjRows.Count == 0)
                {
                    TableRow(t,
                        $"P {(totalLandMv + totalImpMv):N2}",
                        ValueOr(td.AdjustmentFactor, "None (100%)"),
                        $"{td.PercentAdjustment:N2}%",
                        $"P {td.ValueAdjustment:N2}",
                        $"P {totalAdjMv:N2}");
                }
                else
                {
                    foreach (var r in adjRows)
                    {
                        TableRow(t,
                            $"P {r.BaseMarketValue:N2}",
                            ValueOr(r.AdjustmentFactor, "Standard"),
                            $"{r.PercentAdjustment:N2}%",
                            $"P {r.ValueAdjustment:N2}",
                            $"P {r.AdjustedMarketValue:N2}");
                    }
                }
            });
            col.Item().Row(r =>
            {
                r.RelativeItem();
                r.AutoItem().PaddingRight(6).Text("Total   P").FontSize(8f).Bold();
                r.ConstantItem(130).BorderBottom(0.75f).BorderColor(Colors.Black).AlignRight()
                    .Text($"{totalAdjMv:N2}").FontSize(8.5f).ExtraBold();
            });

            // -----------------------------------------------------------------
            // 10. PROPERTY ASSESSMENT (Page 3)
            // -----------------------------------------------------------------
            decimal totalAssessedVal = assRows.Count > 0 ? assRows.Sum(r => r.AssessedValue) : td.AssessedValue;

            col.Item().PaddingTop(2).Text("PROPERTY ASSESSMENT").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.5f);
                });

                TableHeader(t,
                    "Area",
                    "Actual Use",
                    "Adjusted Market\nValue",
                    "Assessment\nLevel (%)",
                    "Assessed\nValue (Php)");

                foreach (var r in assRows)
                {
                    TableRow(t,
                        $"{r.AreaSqm:N2} sq.m.",
                        ValueOr(r.ActualUse, "RESIDENTIAL"),
                        $"P {r.MarketValue:N2}",
                        $"{r.AssessmentLevel:N0}%",
                        $"P {r.AssessedValue:N2}");
                }
            });
            col.Item().Row(r =>
            {
                r.RelativeItem();
                r.AutoItem().PaddingRight(6).Text("Total   P").FontSize(8f).Bold();
                r.ConstantItem(130).BorderBottom(0.75f).BorderColor(Colors.Black).AlignRight()
                    .Text($"{totalAssessedVal:N2}").FontSize(8.5f).ExtraBold().FontColor(Colors.Teal.Darken3);
            });

            // -----------------------------------------------------------------
            // 11. EFFECTIVITY OF ASSESSMENT/REASSESSMENT (Page 3)
            // -----------------------------------------------------------------
            string qtr = (td.EffectivityQuarter ?? "1st").ToUpperInvariant();
            bool q1 = qtr.Contains("1");
            bool q2 = qtr.Contains("2");
            bool q3 = qtr.Contains("3");
            bool q4 = qtr.Contains("4");
            bool isTaxable = !string.Equals(td.Taxability, "Exempt", StringComparison.OrdinalIgnoreCase);
            var backFrom = td.BackTaxesYearFrom?.ToString() ?? "";
            var backTo = td.BackTaxesYearTo?.ToString() ?? "";

            col.Item().PaddingTop(2).Text("EFFECTIVITY OF ASSESSMENT/REASSESSMENT").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(ec =>
            {
                ec.Spacing(4);
                ec.Item().Row(r =>
                {
                    r.RelativeItem(1.2f).Element(e => UnderlinedField(e, "Year:", td.EffectivityYear.ToString()));
                    r.ConstantItem(16);
                    r.AutoItem().PaddingRight(10).Text("Quarter:").FontSize(7.5f);
                    r.AutoItem().PaddingRight(12).Text(CheckBoxText(q1, "1st")).FontSize(7.5f).Bold();
                    r.AutoItem().PaddingRight(12).Text(CheckBoxText(q2, "2nd")).FontSize(7.5f).Bold();
                    r.AutoItem().PaddingRight(12).Text(CheckBoxText(q3, "3rd")).FontSize(7.5f).Bold();
                    r.AutoItem().Text(CheckBoxText(q4, "4th")).FontSize(7.5f).Bold();
                });
                ec.Item().Row(r =>
                {
                    r.AutoItem().PaddingRight(10).Text("Taxability:").FontSize(7.5f);
                    r.AutoItem().PaddingRight(14).Text(CheckBoxText(isTaxable, "Taxable")).FontSize(7.5f).Bold();
                    r.AutoItem().PaddingRight(18).Text(CheckBoxText(!isTaxable, "Exempt")).FontSize(7.5f).Bold();
                    r.RelativeItem().Element(e => UnderlinedField(e, "Basis:", ValueOr(td.TaxabilityBasis, td.AssessmentBasis ?? "RA 7160")));
                });
            });

            col.Item().PaddingHorizontal(4).Row(r =>
            {
                r.AutoItem().PaddingRight(8).Text("Period Covered by Back Taxes (if any):").FontSize(7.5f);
                r.RelativeItem().Element(e => UnderlinedField(e, "Year:", backFrom));
                r.ConstantItem(12);
                r.RelativeItem().Element(e => UnderlinedField(e, "to Year:", backTo));
            });

            // -----------------------------------------------------------------
            // 12. RECORD OF SUPERSEDED ASSESSMENT (Page 3)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(2).Text("RECORD OF SUPERSEDED ASSESSMENT").FontSize(8f).Bold().FontColor(Colors.Black);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Darken2).Padding(5).Column(sc =>
            {
                sc.Spacing(4);
                sc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "Previous TD No.:", ValueOr(td.PreviousTdNo, "")));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "PIN:", ValueOr(td.PreviousPin, "")));
                });
                sc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "Market Value:", td.PreviousMarketValue > 0 ? $"P {td.PreviousMarketValue:N2}" : ""));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "Assessed Value:", td.PreviousAssessedValue > 0 ? $"P {td.PreviousAssessedValue:N2}" : ""));
                });
                sc.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => UnderlinedField(e, "Previous Owner:", ValueOr(td.PreviousOwner, "")));
                    r.ConstantItem(14);
                    r.RelativeItem().Element(e => UnderlinedField(e, "Effectivity of Assessment:", ""));
                });
            });

            // -----------------------------------------------------------------
            // 13. SIGNATORIES: APPRAISED BY, ASSESSED BY, RECOMMENDING APPROVAL BY (Page 3)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(6).Row(r =>
            {
                void SignatoryBlock(IContainer c, string heading, string? name, DateTime? date)
                {
                    c.Column(sc =>
                    {
                        sc.Item().Text(heading).FontSize(7.5f).Bold();
                        sc.Item().PaddingTop(12).Row(sr =>
                        {
                            sr.RelativeItem(2.2f).Column(nc =>
                            {
                                nc.Item().BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter()
                                    .Text(ValueOr(name, " ")).FontSize(7.5f).Bold();
                                nc.Item().AlignCenter().Text("Name and Position").FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken2);
                            });
                            sr.ConstantItem(6);
                            sr.RelativeItem(1f).Column(dc =>
                            {
                                dc.Item().BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter()
                                    .Text((date ?? td.DateAssessed).ToString("MM/dd/yyyy")).FontSize(7.5f);
                                dc.Item().AlignCenter().Text("Date").FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken2);
                            });
                        });
                    });
                }

                r.RelativeItem().Element(c => SignatoryBlock(c, "APPRAISED BY:", td.AppraisedByName, td.DateAppraised));
                r.ConstantItem(12);
                r.RelativeItem().Element(c => SignatoryBlock(c, "ASSESSED BY:", td.AssessedByName, td.DateAssessed));
                r.ConstantItem(12);
                r.RelativeItem().Element(c => SignatoryBlock(c, "RECOMMENDING APPROVAL BY:", td.RecommendingApprovalName, td.DateRecommended));
            });

            // -----------------------------------------------------------------
            // 14. MEMORANDA (Page 3)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(4).Column(mc =>
            {
                mc.Item().Text("MEMORANDA:").FontSize(8f).Bold();
                mc.Item().PaddingTop(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2).PaddingBottom(2)
                    .Text(ValueOr(td.Memoranda, " ")).FontSize(7.5f);
                mc.Item().Height(10).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2);
                mc.Item().Height(10).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken2);
            });

            // -----------------------------------------------------------------
            // 15. APPROVED BY, ENTRY IN ASSESSMENT ROLL BY, ENCODED BY (Page 3)
            // -----------------------------------------------------------------
            col.Item().PaddingTop(6).Column(ac =>
            {
                ac.Spacing(6);

                void BottomSignatoryRow(string label, string? nameVal, string subLabel, DateTime? dateVal)
                {
                    ac.Item().Row(r =>
                    {
                        r.ConstantItem(145).AlignBottom().Text(label).FontSize(7.5f).Bold();
                        r.RelativeItem(2.2f).Column(nc =>
                        {
                            nc.Item().BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter()
                                .Text(ValueOr(nameVal, " ")).FontSize(7.5f).Bold();
                            nc.Item().AlignCenter().Text(subLabel).FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken2);
                        });
                        r.ConstantItem(16);
                        r.RelativeItem(1f).Column(dc =>
                        {
                            dc.Item().BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter()
                                .Text((dateVal ?? td.DateAssessed).ToString("MM/dd/yyyy")).FontSize(7.5f);
                            dc.Item().AlignCenter().Text("Date").FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken2);
                        });
                    });
                }

                string rollEntryFull = string.IsNullOrWhiteSpace(td.EntryInRollPosition)
                    ? ValueOr(td.EntryInRollName, "")
                    : $"{td.EntryInRollName} ({td.EntryInRollPosition})";

                BottomSignatoryRow("APPROVED BY:", td.ApprovedByName, "Provincial/City/Municipal Assessor", td.DateApproved);
                BottomSignatoryRow("Entry in the Assessment Roll by:", rollEntryFull, "Name and Position", td.EntryInRollDate);
                BottomSignatoryRow("Encoded by:", td.EncodedByName, "Name and Position", td.DateEncoded);
            });
        });
    }

    // -------------------------------------------------------------------------
    // MACHINERY MASTER RECORD SECTIONS (ALL FIELDS + 2 TABLES)
    // -------------------------------------------------------------------------
    private static void ComposeMachineryRecordSections(ColumnDescriptor col, TaxDeclaration td)
    {
        // 3A. Land & Building References + Registration / Importation Details
        col.Item().Element(c => SectionBox(c, "3A. MACHINERY REGISTRATION, IMPORTATION & LAND/BUILDING REFERENCE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                KeyValueCell(t, "Manufacture Origin", td.IsLocallyManufactured ? "Locally Manufactured" : "Imported / Foreign");
                KeyValueCell(t, "Machinery Reg. No.", ValueOr(td.MachineryRegNo, "N/A"));
                KeyValueCell(t, "Date Registered", td.MachineryDateRegistered?.ToString("MMM dd, yyyy") ?? "N/A");
                KeyValueCell(t, "Importation Permit No.", ValueOr(td.ImportationPermitNo, "N/A"));
                KeyValueCell(t, "Importation Date Issued", td.ImportationDateIssued?.ToString("MMM dd, yyyy") ?? "N/A");

                KeyValueCell(t, "Country of Origin", ValueOr(td.CountryOfOrigin, "N/A"), topBorder: true);
                KeyValueCell(t, "Supplier / Seller", ValueOr(td.SupplierSeller, "N/A"), topBorder: true);
                KeyValueCell(t, "Supplier Address", ValueOr(td.SupplierAddress, "N/A"), topBorder: true);
                KeyValueCell(t, "Acquisition Cost", $"PHP {td.AcquisitionCost:N2}", topBorder: true);
                KeyValueCell(t, "O.R. No. & Date", ValueOr(td.OrNo, td.OrNoAndDate ?? "N/A"), topBorder: true);

                KeyValueCell(t, "Landowner Name", ValueOr(td.LandOwnerName, td.OwnerName), topBorder: true);
                KeyValueCell(t, "Land TD No. / PIN", $"{ValueOr(td.LandTdNo, "-")} / {ValueOr(td.LandPin, "-")}", topBorder: true);
                KeyValueCell(t, "Building Owner / Ref", ValueOr(td.BuildingReferenceName, "N/A"), topBorder: true);
                KeyValueCell(t, "Building TD No. / PIN", $"{ValueOr(td.BuildingTdNo, "-")} / {ValueOr(td.BuildingPin, "-")}", topBorder: true);
                KeyValueCell(t, "Same Land & Bldg Owner", td.IsLandOwnerAlsoBuildingOwner ? "Yes" : "No", topBorder: true);
            });
        }));

        // 3B. Primary Machinery Specifications
        col.Item().Element(c => SectionBox(c, "3B. PRIMARY MACHINERY SPECIFICATIONS & DEPRECIATION", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                KeyValueCell(t, "Description / Brand", ValueOr(td.DescriptionBrand, "Industrial Equipment"));
                KeyValueCell(t, "Rated Capacity", ValueOr(td.RatedCapacity, "N/A"));
                KeyValueCell(t, "Condition When Acquired", ValueOr(td.ConditionWhenAcquired, "New"));
                KeyValueCell(t, "Date Acquired", td.DateAcquired?.ToString("MMM dd, yyyy") ?? "N/A");
                KeyValueCell(t, "Installed / Operation Yr", $"{td.YearInstalled} / {td.YearOfOperation}");

                KeyValueCell(t, "Economic Life (Est / Rem)", $"{td.EconomicLifeYears} yrs / {td.RemainingEconomicLife} yrs", topBorder: true);
                KeyValueCell(t, "Original Cost", $"PHP {td.OriginalCost:N2}", topBorder: true);
                KeyValueCell(t, "Installation Cost", $"PHP {td.InstallationCost:N2}", topBorder: true);
                KeyValueCell(t, $"Depreciation ({td.DepreciationRatePercent:N1}%)", $"Years Used: {td.YearsUsed}", topBorder: true);
                KeyValueCell(t, "Depreciated Value", $"PHP {td.DepreciatedValue:N2}", topBorder: true);
            });
        }));

        // Table 1: Machinery Appraisal Schedule
        var machRows = GetMachineryDetailRows(td);
        col.Item().Element(c => SectionBox(c, "3C. MACHINERY & EQUIPMENT APPRAISAL SCHEDULE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.8f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(0.9f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1f);
                    c.RelativeColumn(1.4f);
                });

                TableHeader(t, "Class", "Kind / Brand & Model", "Capacity", "Cond.", "Econ. Life", "Original Cost", "Total / RCN", "Depr. %", "Depreciated MV");

                foreach (var m in machRows)
                {
                    var brandKind = string.Join(" - ", new[] { m.KindOfMachinery, m.BrandModel, m.DescriptionBrand }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());
                    TableRow(t,
                        ValueOr(m.Classification, td.Classification.ToString()),
                        ValueOr(brandKind, ValueOr(td.DescriptionBrand, "Machinery")),
                        ValueOr(m.Capacity, td.RatedCapacity),
                        ValueOr(m.ConditionWhenAcquired, "NEW"),
                        $"{m.EconomicLifeEstimated ?? td.EconomicLifeYears}y / {m.RemainingEconomicLife ?? td.RemainingEconomicLife}y",
                        $"PHP {m.OriginalCost:N2}",
                        $"PHP {(m.RCN > 0 ? m.RCN : m.TotalCost):N2}",
                        $"{m.DepreciationTotalPercentage:N1}%",
                        $"PHP {m.DepreciatedValue:N2}");
                }
            });
        }));

        // Table 2: Machinery Property Assessment Schedule
        var machAssRows = GetMachineryAssessmentRows(td);
        col.Item().Element(c => SectionBox(c, "3D. MACHINERY PROPERTY ASSESSMENT SUMMARY SCHEDULE", inner =>
        {
            inner.Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2f);
                    c.RelativeColumn(2f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(2f);
                });

                TableHeader(t, "Actual Use", "Market Value (PHP)", "Assessment Level (%)", "Assessed Value (PHP)");

                foreach (var a in machAssRows)
                {
                    TableRow(t,
                        ValueOr(a.ActualUseName, td.ActualUse),
                        $"PHP {a.MarketValue:N2}",
                        $"{a.AssessmentLevelPercent:N0}%",
                        $"PHP {a.AssessmentValue:N2}");
                }
            });
        }));
    }

    // =========================================================================
    // PART 2: 1-PAGE OFFICIAL TAX DECLARATION CERTIFICATE (FOR REQUESTS)
    // =========================================================================
    private static void ComposeCertificateHeader(IContainer container, TaxDeclaration td, string? trackingNo, string? transactionLabel)
    {
        container.Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(trackingNo))
            {
                col.Item().PaddingBottom(3).Row(r =>
                {
                    r.RelativeItem().Text($"Request Ref: {trackingNo}").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(transactionLabel))
                    {
                        r.AutoItem().Text(transactionLabel).FontSize(7.5f).Bold().FontColor(Colors.Teal.Darken3);
                    }
                });
            }

            col.Item().AlignCenter().Text("REPUBLIC OF THE PHILIPPINES").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text($"City / Municipality of {ValueOr(td.Municipality, "General Santos City")}").FontSize(8.5f).SemiBold();
            col.Item().AlignCenter().Text("OFFICE OF THE CITY ASSESSOR").FontSize(12).ExtraBold().FontColor(Colors.Black);
            col.Item().PaddingTop(2).AlignCenter().Text("TAX DECLARATION OF REAL PROPERTY").FontSize(13).Black().FontColor(Colors.Teal.Darken3);
            col.Item().PaddingBottom(6).AlignCenter().Text("(Issued pursuant to Section 222 of Republic Act No. 7160)").FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1);

            if (td.IsCancelledAndSuperseded || td.Status == AssessmentStatus.Cancelled)
            {
                col.Item().PaddingBottom(6).Border(1.5f).BorderColor(Colors.Red.Medium).Background(Colors.Red.Lighten5).Padding(6).Column(cCol =>
                {
                    cCol.Item().AlignCenter().Text("*** CANCELLED & SUPERSEDED ***").FontSize(10).Black().FontColor(Colors.Red.Darken3);
                    var reason = string.IsNullOrWhiteSpace(td.CancellationReason) ? "" : $" due to {td.CancellationReason}";
                    var dateStr = (td.DateCancelled ?? td.DateAssessed).ToString("MMMM dd, yyyy");
                    var supStr = string.IsNullOrWhiteSpace(td.SupersededByTdns) ? "" : $" Superseded by TD(s): {td.SupersededByTdns}.";
                    cCol.Item().AlignCenter().Text($"This Tax Declaration was cancelled on {dateStr}{reason}.{supStr}").FontSize(8).SemiBold().FontColor(Colors.Red.Darken2);
                });
            }

            col.Item().Border(1).BorderColor(Colors.Grey.Darken2).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
            {
                row.RelativeItem(3).Column(c =>
                {
                    c.Item().Text("TD No.").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.TaxDeclarationNumber, "N/A")).FontSize(11).Black().FontColor(Colors.Black);
                });
                row.RelativeItem(3).Column(c =>
                {
                    c.Item().Text("Property Identification No. (PIN)").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.PropertyIndexNumber, "N/A")).FontSize(10).ExtraBold().FontColor(Colors.Black);
                });
                row.RelativeItem(3).Column(c =>
                {
                    c.Item().Text("ARP No.").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.ARPNo, "N/A")).FontSize(9.5f).Bold();
                });
                row.RelativeItem(3).Column(c =>
                {
                    c.Item().Text("Transaction Code").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.TransactionCodeDisplay, td.TransactionCode)).FontSize(8.5f).Bold();
                });
            });
        });
    }

    private static void ComposeCertificateContent(IContainer container, TaxDeclaration td)
    {
        container.PaddingTop(6).Column(col =>
        {
            col.Spacing(6);

            // 1. Owner & Administrator Information
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1.5f);
                });

                table.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Owner:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.OwnerName, "N/A")).FontSize(9.5f).Bold();
                });
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("TIN:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.OwnerTin, "N/A")).FontSize(8.5f).SemiBold();
                });

                table.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Address:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.OwnerAddress, "N/A")).FontSize(8.5f);
                });
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Telephone No.:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.OwnerPhone, "N/A")).FontSize(8.5f);
                });

                table.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Administrator / Beneficial User:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.AdministratorName, "N/A")).FontSize(8.5f).SemiBold();
                });
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("TIN:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.AdministratorTin, "N/A")).FontSize(8.5f);
                });

                table.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Address:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.AdministratorAddress, "N/A")).FontSize(8.5f);
                });
                table.Cell().Padding(4).Column(c =>
                {
                    c.Item().Text("Telephone No.:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.AdministratorContactNo, "N/A")).FontSize(8.5f);
                });
            });

            // 2. Location of Property
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Column(locCol =>
            {
                locCol.Item().Background(Colors.Grey.Lighten3).PaddingHorizontal(5).PaddingVertical(2.5f)
                    .Text("LOCATION OF PROPERTY").FontSize(7.5f).ExtraBold().FontColor(Colors.Grey.Darken3);

                locCol.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(2f);
                    });

                    t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                    {
                        c.Item().Text(ValueOr(td.StreetNo, "N/A")).FontSize(8.5f).Bold();
                        c.Item().Text("(Number and Street)").FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken1);
                    });
                    t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                    {
                        c.Item().Text(ValueOr(td.DistrictOrBarangay, "N/A")).FontSize(8.5f).Bold();
                        c.Item().Text("(Barangay / District)").FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken1);
                    });
                    t.Cell().Padding(4).Column(c =>
                    {
                        var munProv = string.Join(", ", new[] { td.Municipality, td.ProvinceCity }.Where(s => !string.IsNullOrWhiteSpace(s)));
                        c.Item().Text(ValueOr(munProv, "General Santos City")).FontSize(8.5f).Bold();
                        c.Item().Text("(Municipality & Province/City)").FontSize(6.5f).Italic().FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            // 3. Title, Survey, Cadastral & Lot Info
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("OCT/TCT/CLOA No.:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    var titleLabel = string.IsNullOrWhiteSpace(td.TitleNo) ? "N/A" : $"{td.TitleType}: {td.TitleNo}";
                    c.Item().Text(titleLabel).FontSize(8.5f).SemiBold();
                });
                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("CCT:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(td.IsCct ? ValueOr(td.CctTitleNo, "Yes") : "N/A").FontSize(8.5f);
                });
                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Dated:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(td.DateRegistered?.ToString("MMM dd, yyyy") ?? "N/A").FontSize(8.5f);
                });
                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Survey No.:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.SurveyNo, td.CadastralSurveyNo ?? "N/A")).FontSize(8.5f).SemiBold();
                });

                t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Lot No.:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.LotNo, td.LandLotNo ?? "N/A")).FontSize(8.5f).SemiBold();
                });
                t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Block No.:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.BlockNo, td.LandBlockNo ?? "N/A")).FontSize(8.5f).SemiBold();
                });
                t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                {
                    c.Item().Text("Section / Parcel:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text($"{ValueOr(td.SectionNo, "-")} / {ValueOr(td.ParcelNo, "-")}").FontSize(8.5f);
                });
                t.Cell().Padding(4).Column(c =>
                {
                    c.Item().Text("Registration Status:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(ValueOr(td.RegistrationStatus, "Titled")).FontSize(8.5f);
                });
            });

            // 4. Boundaries
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Column(bCol =>
            {
                bCol.Item().Background(Colors.Grey.Lighten3).PaddingHorizontal(5).PaddingVertical(2.5f)
                    .Text("BOUNDARIES").FontSize(7.5f).ExtraBold().FontColor(Colors.Grey.Darken3);

                bCol.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4)
                        .Text(txt =>
                        {
                            txt.Span("North: ").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken2);
                            txt.Span(ValueOr(td.BoundaryNorth, "N/A")).FontSize(8.5f);
                        });
                    t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4)
                        .Text(txt =>
                        {
                            txt.Span("East: ").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken2);
                            txt.Span(ValueOr(td.BoundaryEast, "N/A")).FontSize(8.5f);
                        });
                    t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4)
                        .Text(txt =>
                        {
                            txt.Span("South: ").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken2);
                            txt.Span(ValueOr(td.BoundarySouth, "N/A")).FontSize(8.5f);
                        });
                    t.Cell().Padding(4)
                        .Text(txt =>
                        {
                            txt.Span("West: ").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken2);
                            txt.Span(ValueOr(td.BoundaryWest, "N/A")).FontSize(8.5f);
                        });
                });
            });

            // 5. Kind of Property Assessed
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Column(kCol =>
            {
                kCol.Item().Background(Colors.Grey.Lighten3).PaddingHorizontal(5).PaddingVertical(2.5f)
                    .Text("KIND OF PROPERTY ASSESSED").FontSize(7.5f).ExtraBold().FontColor(Colors.Grey.Darken3);

                kCol.Item().Padding(5).Column(inner =>
                {
                    inner.Spacing(4);
                    inner.Item().Row(r =>
                    {
                        r.RelativeItem().Text(td.Kind == PropertyKind.Land ? "[ X ] LAND" : "[   ] LAND").FontSize(8.5f).Bold();
                        r.RelativeItem().Text(td.Kind == PropertyKind.Building ? "[ X ] BUILDING" : "[   ] BUILDING").FontSize(8.5f).Bold();
                        r.RelativeItem().Text(td.Kind == PropertyKind.Machinery ? "[ X ] MACHINERY" : "[   ] MACHINERY").FontSize(8.5f).Bold();
                        r.RelativeItem().Text("[   ] OTHERS").FontSize(8.5f).Bold();
                    });

                    if (td.Kind == PropertyKind.Building)
                    {
                        inner.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text($"No. of Storeys: {td.NoOfStoreys}").FontSize(8);
                            r.RelativeItem(2).Text($"Structural Type / Class: {td.StructuralType} ({td.BuildingClassification})").FontSize(8);
                            r.RelativeItem().Text($"Land TD No.: {ValueOr(td.LandTdNo, "N/A")}").FontSize(8);
                        });
                    }
                    else if (td.Kind == PropertyKind.Machinery)
                    {
                        inner.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem(2).Text($"Description / Brand: {ValueOr(td.DescriptionBrand, "Industrial Machinery")}").FontSize(8);
                            r.RelativeItem().Text($"Capacity: {ValueOr(td.RatedCapacity, "N/A")}").FontSize(8);
                            r.RelativeItem().Text($"Land TD No.: {ValueOr(td.LandTdNo, "N/A")}").FontSize(8);
                        });
                    }
                });
            });

            // 6. Valuation & Assessment Breakdown Table
            var rows = BuildAssessmentRows(td);
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Column(vCol =>
            {
                vCol.Item().Background(Colors.Grey.Lighten3).PaddingHorizontal(5).PaddingVertical(2.5f)
                    .Text("PROPERTY APPRAISAL & ASSESSMENT SUMMARY").FontSize(7.5f).ExtraBold().FontColor(Colors.Grey.Darken3);

                vCol.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(1.8f);
                        c.RelativeColumn(1.8f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1.8f);
                    });

                    t.Header(h =>
                    {
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(4).Text("Classification").FontSize(7).Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("Area (sq.m.)").FontSize(7).Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("Market Value").FontSize(7).Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(4).AlignCenter().Text("Actual Use").FontSize(7).Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(4).AlignCenter().Text("Ass. Level").FontSize(7).Bold();
                        h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("Assessed Value").FontSize(7).Bold();
                    });

                    foreach (var r in rows)
                    {
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(r.Classification).FontSize(8).SemiBold();
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(r.AreaSqm > 0 ? $"{r.AreaSqm:N2}" : "-").FontSize(8);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"PHP {r.MarketValue:N2}").FontSize(8);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(r.ActualUse).FontSize(8);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text($"{r.AssessmentLevel:N0}%").FontSize(8);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"PHP {r.AssessedValue:N2}").FontSize(8).Bold();
                    }

                    t.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("TOTAL").FontSize(8).ExtraBold();
                    t.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text(td.AreaSqMeters > 0 ? $"{td.AreaSqMeters:N2} sq.m." : "-").FontSize(8).Bold();
                    t.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"PHP {td.AdjustedMarketValue:N2}").FontSize(8.5f).ExtraBold();
                    t.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("");
                    t.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("");
                    t.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"PHP {td.AssessedValue:N2}").FontSize(9).Black().FontColor(Colors.Teal.Darken3);
                });

                vCol.Item().BorderTop(0.5f).BorderColor(Colors.Grey.Medium).Padding(4).Row(r =>
                {
                    r.AutoItem().Text("Total Assessed Value (Amount in Words): ").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken2);
                    r.RelativeItem().Text(AmountToWords(td.AssessedValue)).FontSize(8).Bold().Italic();
                });
            });

            // 7. Taxability, Effectivity & Annual Tax Liability
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Row(row =>
            {
                var isTaxable = !string.Equals(td.Taxability, "Exempt", StringComparison.OrdinalIgnoreCase);
                row.RelativeItem(1.2f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(c =>
                {
                    c.Spacing(3);
                    c.Item().Text("TAXABILITY & EFFECTIVITY").FontSize(7.5f).ExtraBold().FontColor(Colors.Grey.Darken3);
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text(isTaxable ? "[ X ] Taxable" : "[   ] Taxable").FontSize(8.5f).Bold();
                        r.RelativeItem().Text(!isTaxable ? "[ X ] Exempt" : "[   ] Exempt").FontSize(8.5f).Bold();
                    });
                    c.Item().PaddingTop(2).Text($"Effectivity of Assessment/Reassessment: {ValueOr(td.EffectivityQuarter, "1st Quarter")} {td.EffectivityYear}").FontSize(8).SemiBold();
                    c.Item().Text($"Legal Basis: {ValueOr(td.TaxabilityBasis, "RA 7160 Local Government Code")}").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                });

                var basicTax = Math.Round(td.AssessedValue * (td.BasicTaxRate / 100m), 2);
                var sefTax = Math.Round(td.AssessedValue * (td.SefTaxRate / 100m), 2);
                row.RelativeItem(1f).Padding(5).Column(c =>
                {
                    c.Spacing(2);
                    c.Item().Text("ESTIMATED ANNUAL RPT LIABILITY").FontSize(7.5f).ExtraBold().FontColor(Colors.Grey.Darken3);
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Basic RPT ({td.BasicTaxRate:N1}%):").FontSize(8);
                        r.AutoItem().Text($"PHP {basicTax:N2}").FontSize(8).SemiBold();
                    });
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Special Education Fund - SEF ({td.SefTaxRate:N1}%):").FontSize(8);
                        r.AutoItem().Text($"PHP {sefTax:N2}").FontSize(8).SemiBold();
                    });
                    c.Item().PaddingTop(2).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1).Row(r =>
                    {
                        r.RelativeItem().Text("Total Annual Tax Due:").FontSize(8.5f).ExtraBold().FontColor(Colors.Teal.Darken3);
                        r.AutoItem().Text($"PHP {td.AnnualTaxDue:N2}").FontSize(9).Black().FontColor(Colors.Teal.Darken3);
                    });
                });
            });

            // 8. Signatories
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(c =>
                {
                    c.Item().Text("Appraised / Assessed By:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().PaddingTop(14).Row(r =>
                    {
                        r.RelativeItem(2).Column(sc =>
                        {
                            sc.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).AlignCenter().Text(ValueOr(td.AssessedByName, td.AppraisedByName)).FontSize(8.5f).Bold();
                            sc.Item().AlignCenter().Text("Assessment Officer / Appraiser").FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                        });
                        r.ConstantItem(8);
                        r.RelativeItem(1).Column(sc =>
                        {
                            sc.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).AlignCenter().Text(td.DateAssessed.ToString("MM/dd/yyyy")).FontSize(8);
                            sc.Item().AlignCenter().Text("Date").FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(c =>
                {
                    c.Item().Text("Recommending Approval:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().PaddingTop(14).Row(r =>
                    {
                        r.RelativeItem(2).Column(sc =>
                        {
                            sc.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).AlignCenter().Text(ValueOr(td.RecommendingApprovalName, "Assistant City Assessor")).FontSize(8.5f).Bold();
                            sc.Item().AlignCenter().Text("Assistant City Assessor").FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                        });
                        r.ConstantItem(8);
                        r.RelativeItem(1).Column(sc =>
                        {
                            sc.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).AlignCenter().Text((td.DateRecommended ?? td.DateAssessed).ToString("MM/dd/yyyy")).FontSize(8);
                            sc.Item().AlignCenter().Text("Date").FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                t.Cell().ColumnSpan(2).Padding(6).Column(c =>
                {
                    c.Item().Text("APPROVED BY:").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().PaddingTop(16).AlignCenter().Width(280).Row(r =>
                    {
                        r.RelativeItem(2).Column(sc =>
                        {
                            sc.Item().BorderBottom(0.75f).BorderColor(Colors.Black).AlignCenter().Text(ValueOr(td.ApprovedByName, "Hon. City Assessor")).FontSize(9.5f).ExtraBold();
                            sc.Item().AlignCenter().Text("Provincial / City / Municipal Assessor").FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                        });
                        r.ConstantItem(12);
                        r.RelativeItem(1).Column(sc =>
                        {
                            sc.Item().BorderBottom(0.75f).BorderColor(Colors.Black).AlignCenter().Text((td.DateApproved ?? td.DateAssessed).ToString("MM/dd/yyyy")).FontSize(8.5f).SemiBold();
                            sc.Item().AlignCenter().Text("Date").FontSize(7).FontColor(Colors.Grey.Darken2);
                        });
                    });
                });
            });

            // 9. Previous TD Reference & Memoranda
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Medium).Padding(5).Column(mCol =>
            {
                mCol.Spacing(4);
                mCol.Item().Row(r =>
                {
                    r.RelativeItem().Text($"This declaration cancels TD No.: {ValueOr(td.PreviousTdNo, "None / New")}").FontSize(8).SemiBold();
                    r.RelativeItem().Text($"Previous Owner: {ValueOr(td.PreviousOwner, "N/A")}").FontSize(8).SemiBold();
                    r.RelativeItem().AlignRight().Text($"Previous A.V.: PHP {td.PreviousAssessedValue:N2}").FontSize(8).SemiBold();
                });

                mCol.Item().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingTop(3).Column(mc =>
                {
                    mc.Item().Text("MEMORANDA:").FontSize(7.5f).ExtraBold().FontColor(Colors.Grey.Darken3);
                    mc.Item().Text(ValueOr(td.Memoranda, "Standard real property assessment encoded pursuant to City Tax Ordinance.")).FontSize(8);
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, TaxDeclaration td)
    {
        container.PaddingTop(6).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1).Row(row =>
        {
            row.RelativeItem().Text(txt =>
            {
                txt.Span("Encoded by: ").FontSize(7).FontColor(Colors.Grey.Darken1);
                txt.Span(ValueOr(td.EncodedByName, "System")).FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                txt.Span($"  |  Printed: {DateTime.Now:MMM dd, yyyy hh:mm tt}").FontSize(7).FontColor(Colors.Grey.Darken1);
            });

            row.AutoItem().Text(txt =>
            {
                txt.Span("Page ").FontSize(7).FontColor(Colors.Grey.Darken1);
                txt.CurrentPageNumber().FontSize(7).Bold();
                txt.Span(" of ").FontSize(7).FontColor(Colors.Grey.Darken1);
                txt.TotalPages().FontSize(7).Bold();
            });
        });
    }

    // =========================================================================
    // HELPER BUILDERS FOR TABLES & CELLS
    // =========================================================================
    private static void SectionBox(IContainer container, string title, Action<IContainer> contentBuilder)
    {
        container.Border(0.75f).BorderColor(Colors.Grey.Medium).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium)
                .PaddingHorizontal(5).PaddingVertical(2.5f)
                .Text(title).FontSize(7.5f).ExtraBold().FontColor(Colors.Teal.Darken4);
            col.Item().Element(contentBuilder);
        });
    }

    private static void KeyValueCell(TableDescriptor t, string label, string value, bool topBorder = false)
    {
        var cell = t.Cell().BorderRight(0.5f).BorderColor(Colors.Grey.Lighten2);
        if (topBorder) cell = cell.BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2);
        cell.Padding(3.5f).Column(c =>
        {
            c.Item().Text(label).FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken2);
            c.Item().Text(ValueOr(value, "N/A")).FontSize(7.5f).SemiBold().FontColor(Colors.Black);
        });
    }

    private static void TableHeader(TableDescriptor t, params string[] headers)
    {
        t.Header(h =>
        {
            foreach (var text in headers)
            {
                h.Cell().BorderBottom(0.75f).BorderRight(0.5f).BorderColor(Colors.Grey.Medium)
                    .Background(Colors.Grey.Lighten4).Padding(3.5f)
                    .Text(text).FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken3);
            }
        });
    }

    private static void TableRow(TableDescriptor t, params string[] cells)
    {
        foreach (var text in cells)
        {
            t.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten2)
                .Padding(3.5f).Text(text ?? "-").FontSize(7.5f);
        }
    }

    private static List<LandClassificationItemDto> GetLandAppraisalRows(TaxDeclaration td)
    {
        if (td.LandDetails?.TDNLandDetails != null && td.LandDetails.TDNLandDetails.Count > 0)
        {
            return td.LandDetails.TDNLandDetails.OrderBy(d => d.OrderNo).Select(d => new LandClassificationItemDto
            {
                ClassificationName = d.ClassificationName,
                SubClassName = d.SubClassName,
                ActualUseName = d.ActualUseName,
                AreaSQM = d.AreaSQM,
                UnitValue = d.UnitValue,
                IsTaxable = d.IsTaxable
            }).ToList();
        }
        if (!string.IsNullOrWhiteSpace(td.LandAppraisalDetailsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<LandClassificationItemDto>>(td.LandAppraisalDetailsJson, _jsonOpts);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        return new List<LandClassificationItemDto>
        {
            new()
            {
                ClassificationName = td.Classification.ToString().ToUpperInvariant(),
                SubClassName = ValueOr(td.SubClassification, "1st Class"),
                ActualUseName = ValueOr(td.ActualUse, td.Classification.ToString().ToUpperInvariant()),
                AreaSQM = td.AreaSqMeters,
                UnitValue = td.UnitValue,
                IsTaxable = !string.Equals(td.Taxability, "Exempt", StringComparison.OrdinalIgnoreCase)
            }
        };
    }

    private static List<LandImprovementItemDto> GetLandImprovementRows(TaxDeclaration td)
    {
        if (td.LandDetails?.TDNLandImprovements != null && td.LandDetails.TDNLandImprovements.Count > 0)
        {
            return td.LandDetails.TDNLandImprovements.OrderBy(i => i.OrderNo).Select(i => new LandImprovementItemDto
            {
                ProductClass = i.ProductClass,
                AreaPlanted = i.AreaPlanted,
                IsFruitBearing = i.IsFruitBearing,
                Quantity = i.Quantity,
                AgeYears = i.AgeYears,
                UnitValue = i.UnitValue,
                AssessmentLevel = i.AssessmentLevel,
                IsTaxable = i.IsTaxable
            }).ToList();
        }
        if (!string.IsNullOrWhiteSpace(td.LandImprovementsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<LandImprovementItemDto>>(td.LandImprovementsJson, _jsonOpts);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        if (td.ImprovementQty > 0 || td.ImprovementMarketValue > 0)
        {
            return new List<LandImprovementItemDto>
            {
                new()
                {
                    ProductClass = ValueOr(td.ImprovementType, "Improvement"),
                    Quantity = td.ImprovementQty,
                    UnitValue = td.ImprovementUnitValue,
                    AssessmentLevel = td.AssessmentLevelPercent
                }
            };
        }
        return new List<LandImprovementItemDto>();
    }

    private static List<LandAdjustmentItemDto> GetLandAdjustmentRows(TaxDeclaration td)
    {
        if (td.LandDetails?.TDNLandBaseMarkets != null && td.LandDetails.TDNLandBaseMarkets.Count > 0)
        {
            return td.LandDetails.TDNLandBaseMarkets.OrderBy(b => b.OrderNo).Select(b => new LandAdjustmentItemDto
            {
                ClassificationName = b.ClassificationName,
                BaseMarketValue = b.BaseMarketValue,
                AdjustmentFactor = b.AdjustmentFactor,
                PercentAdjustment = b.PercentAdjustment
            }).ToList();
        }
        if (!string.IsNullOrWhiteSpace(td.LandAdjustmentsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<LandAdjustmentItemDto>>(td.LandAdjustmentsJson, _jsonOpts);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        return new List<LandAdjustmentItemDto>();
    }

    private static List<BuildingFloorItem> GetBuildingFloorRows(TaxDeclaration td)
    {
        if (td.BuildingDetails?.FloorLevels != null && td.BuildingDetails.FloorLevels.Count > 0)
        {
            return td.BuildingDetails.FloorLevels.OrderBy(f => f.FloorOrder).Select(f => new BuildingFloorItem
            {
                FloorNumber = f.FloorNumber,
                Description = f.Description,
                FloorAreaSqm = f.FloorAreaSqm,
                BaseUnitCost = f.BaseUnitCost
            }).ToList();
        }
        if (!string.IsNullOrWhiteSpace(td.BuildingDetails?.FloorBreakdownJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<BuildingFloorItem>>(td.BuildingDetails.FloorBreakdownJson, _jsonOpts);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        return new List<BuildingFloorItem>
        {
            new()
            {
                FloorNumber = ValueOr(td.FloorLevel, "1st Floor"),
                Description = ValueOr(td.BuildingUseType, "Main Building Floor"),
                FloorAreaSqm = td.TotalBuildingAreaSqm > 0 ? td.TotalBuildingAreaSqm : td.AreaSqMeters,
                BaseUnitCost = td.UnitValue > 0 ? td.UnitValue : (td.AreaSqMeters > 0 ? Math.Round(td.MarketValue / td.AreaSqMeters, 2) : td.MarketValue)
            }
        };
    }

    private static List<(string Part, string Material)> GetBuildingMaterialRows(TaxDeclaration td)
    {
        if (td.BuildingDetails?.StructuralMaterials != null && td.BuildingDetails.StructuralMaterials.Count > 0)
        {
            return td.BuildingDetails.StructuralMaterials
                .OrderBy(m => m.PartNumber)
                .Select(m => (
                    ValueOr(m.PartName, $"Part {m.PartNumber}"),
                    string.IsNullOrWhiteSpace(m.SpecificDescription) || (m.MaterialSelected != null && m.MaterialSelected.Contains($"({m.SpecificDescription})", StringComparison.OrdinalIgnoreCase))
                        ? ValueOr(m.MaterialSelected, "Standard")
                        : $"{m.MaterialSelected} ({m.SpecificDescription})"
                ))
                .ToList();
        }

        return new List<(string Part, string Material)>
        {
            ("1. Foundation", ValueOr(td.StructuralFoundation, "Reinforced Concrete (RC)")),
            ("2. Columns", ValueOr(td.StructuralColumns, "Reinforced Concrete (RC)")),
            ("3. Beams", ValueOr(td.StructuralBeams, "Reinforced Concrete (RC)")),
            ("4. Roof Framing", ValueOr(td.StructuralTrussRoof, "Steel (S)")),
            ("5. Roofing", ValueOr(td.StructuralRoof, "Long Span G.I. Sheet-Pre painted (LGI)")),
            ("6. Exterior Walls", ValueOr(td.StructuralExteriorWalls, "Concrete Hollow Block (CHB)")),
            ("7. Flooring", ValueOr(td.StructuralFlooring, "Reinforced Concrete (RC)")),
            ("8. Doors", ValueOr(td.DoorMain, "Wood Panel (WP)")),
            ("9. Ceiling", ValueOr(td.Ceiling, "Hardiflex (Hx)")),
            ("10. Windows", ValueOr(td.StructuralWindows, "Aluminum Casement (AC)")),
            ("11. Stairs", ValueOr(td.StructuralStairs, "Reinforced concrete (RC)")),
            ("12. Partition", ValueOr(td.StructuralPartitionWalls, "Hardiflex / Dry wall (Hx)")),
            ("13. Wall Finish", ValueOr(td.StructuralFinishFront, "Cement Plaster (CP)")),
            ("14. Electrical Conduit", ValueOr(td.ElectricalSystem, "Poly Vinyl Carbonate Conduit (EPVC)")),
            ("15. Toilet and Bath", ValueOr(td.ToiletAndBathroom, "Cement Tiles (CT)")),
            ("16. Plumbing and Services", ValueOr(td.PlumbingSewer, "Poly Vinyl Carbonate Pipe (PVCP)"))
        };
    }

    private static List<BuildingAdditionalItemDto> GetBuildingAdditionalRows(TaxDeclaration td)
    {
        if (td.BuildingDetails?.AdditionalItems != null && td.BuildingDetails.AdditionalItems.Count > 0)
        {
            return td.BuildingDetails.AdditionalItems.Select(a => new BuildingAdditionalItemDto
            {
                ItemDescription = a.ItemDescription,
                Quantity = a.Quantity,
                UnitRate = a.UnitRate,
                MultiplierPercent = a.MultiplierPercent,
                RateType = a.RateType,
                TotalValue = a.TotalValue
            }).ToList();
        }
        if (!string.IsNullOrWhiteSpace(td.BuildingDetails?.AdditionalItemsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<BuildingAdditionalItemDto>>(td.BuildingDetails.AdditionalItemsJson, _jsonOpts);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        return new List<BuildingAdditionalItemDto>();
    }

    private static List<MachineryDetailItemDto> GetMachineryDetailRows(TaxDeclaration td)
    {
        if (td.MachineryDetails?.TDNMachineryDetails != null && td.MachineryDetails.TDNMachineryDetails.Count > 0)
        {
            return td.MachineryDetails.TDNMachineryDetails.OrderBy(d => d.OrderNo).Select(d => new MachineryDetailItemDto
            {
                Classification = d.Classification,
                KindOfMachinery = d.KindOfMachinery,
                BrandModel = d.BrandModel,
                Capacity = d.Capacity,
                ConditionWhenAcquired = d.ConditionWhenAcquired,
                EconomicLifeEstimated = d.EconomicLifeEstimated,
                RemainingEconomicLife = d.RemainingEconomicLife,
                YearInstalled = d.YearInstalled,
                YearOfOperation = d.YearOfOperation,
                OriginalCost = d.OriginalCost,
                InstallationCost = d.InstallationCost,
                TotalCost = d.TotalCost,
                RCN = d.RCN,
                NoOfYearsUsed = d.NoOfYearsUsed,
                MarketValue = d.MarketValue,
                DepreciationTotalPercentage = d.DepreciationTotalPercentage,
                DepreciationTotalValue = d.DepreciationTotalValue,
                DepreciatedValue = d.DepreciatedValue
            }).ToList();
        }
        if (!string.IsNullOrWhiteSpace(td.MachineryAppraisalDetailsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<MachineryDetailItemDto>>(td.MachineryAppraisalDetailsJson, _jsonOpts);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        return new List<MachineryDetailItemDto>
        {
            new()
            {
                Classification = td.Classification.ToString(),
                DescriptionBrand = ValueOr(td.DescriptionBrand, "Industrial Machinery"),
                Capacity = ValueOr(td.RatedCapacity, "N/A"),
                ConditionWhenAcquired = ValueOr(td.ConditionWhenAcquired, "NEW"),
                EconomicLifeEstimated = td.EconomicLifeYears,
                RemainingEconomicLife = td.RemainingEconomicLife,
                YearInstalled = td.YearInstalled,
                YearOfOperation = td.YearOfOperation,
                OriginalCost = td.OriginalCost > 0 ? td.OriginalCost : td.MarketValue,
                InstallationCost = td.InstallationCost,
                TotalCost = td.TotalCost > 0 ? td.TotalCost : td.MarketValue,
                RCN = td.TotalCost > 0 ? td.TotalCost : td.MarketValue,
                DepreciationTotalPercentage = td.DepreciationRatePercent,
                DepreciatedValue = td.DepreciatedValue > 0 ? td.DepreciatedValue : td.MarketValue
            }
        };
    }

    private static List<MachineryPropertyAssessmentItemDto> GetMachineryAssessmentRows(TaxDeclaration td)
    {
        if (td.MachineryDetails?.TDNMachineryPropertyAssessments != null && td.MachineryDetails.TDNMachineryPropertyAssessments.Count > 0)
        {
            return td.MachineryDetails.TDNMachineryPropertyAssessments.OrderBy(a => a.OrderNo).Select(a => new MachineryPropertyAssessmentItemDto
            {
                ActualUseName = a.ActualUseName,
                MarketValue = a.MarketValue,
                AssessmentLevelPercent = a.AssessmentLevelPercent
            }).ToList();
        }
        if (!string.IsNullOrWhiteSpace(td.MachineryPropertyAssessmentsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<MachineryPropertyAssessmentItemDto>>(td.MachineryPropertyAssessmentsJson, _jsonOpts);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        return new List<MachineryPropertyAssessmentItemDto>
        {
            new()
            {
                ActualUseName = ValueOr(td.ActualUse, "INDUSTRIAL"),
                MarketValue = td.MarketValue,
                AssessmentLevelPercent = td.AssessmentLevelPercent
            }
        };
    }

    private sealed record AssessmentRow(string Classification, decimal AreaSqm, decimal MarketValue, string ActualUse, decimal AssessmentLevel, decimal AssessedValue);

    private static List<AssessmentRow> BuildAssessmentRows(TaxDeclaration td)
    {
        var list = new List<AssessmentRow>();

        if (td.LandDetails?.TDNLandPropertyAssessments != null && td.LandDetails.TDNLandPropertyAssessments.Count > 0)
        {
            foreach (var i in td.LandDetails.TDNLandPropertyAssessments.OrderBy(x => x.OrderNo))
            {
                list.Add(new AssessmentRow(
                    ValueOr(i.Kinds, td.Classification.ToString().ToUpperInvariant()),
                    td.AreaSqMeters,
                    i.MarketValue,
                    ValueOr(i.ActualUseName, td.ActualUse),
                    i.AssessmentLevelPercent,
                    i.AssessmentValue));
            }
        }
        else if (!string.IsNullOrWhiteSpace(td.LandPropertyAssessmentsJson))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<LandPropertyAssessmentItemDto>>(td.LandPropertyAssessmentsJson, _jsonOpts);
                if (items != null && items.Count > 0)
                {
                    foreach (var i in items)
                    {
                        list.Add(new AssessmentRow(
                            ValueOr(i.Kinds, td.Classification.ToString().ToUpperInvariant()),
                            td.AreaSqMeters,
                            i.MarketValue,
                            ValueOr(i.ActualUseName, td.ActualUse),
                            i.AssessmentLevelPercent,
                            i.AssessmentValue));
                    }
                }
            }
            catch { }
        }

        if (list.Count == 0)
        {
            list.Add(new AssessmentRow(
                td.Classification.ToString().ToUpperInvariant(),
                td.AreaSqMeters,
                td.AdjustedMarketValue > 0 ? td.AdjustedMarketValue : td.MarketValue,
                ValueOr(td.ActualUse, td.Classification.ToString()),
                td.AssessmentLevelPercent,
                td.AssessedValue));
        }

        return list;
    }

    private static string ValueOr(string? val, string fallback) =>
        string.IsNullOrWhiteSpace(val) ? fallback : val.Trim();

    private static string AmountToWords(decimal amount)
    {
        if (amount <= 0) return "ZERO PESOS ONLY";
        long pesos = (long)Math.Floor(amount);
        int centavos = (int)Math.Round((amount - pesos) * 100);
        string words = NumberToWords(pesos).ToUpperInvariant() + " PESOS";
        if (centavos > 0)
            words += $" AND {centavos:00}/100";
        return words + " ONLY";
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "zero";
        if (number < 0) return "minus " + NumberToWords(Math.Abs(number));

        string words = "";
        if ((number / 1000000000) > 0)
        {
            words += NumberToWords(number / 1000000000) + " billion ";
            number %= 1000000000;
        }
        if ((number / 1000000) > 0)
        {
            words += NumberToWords(number / 1000000) + " million ";
            number %= 1000000;
        }
        if ((number / 1000) > 0)
        {
            words += NumberToWords(number / 1000) + " thousand ";
            number %= 1000;
        }
        if ((number / 100) > 0)
        {
            words += NumberToWords(number / 100) + " hundred ";
            number %= 100;
        }
        if (number > 0)
        {
            var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
            var tensMap = new[] { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += "-" + unitsMap[number % 10];
            }
        }
        return words.Trim();
    }
}
