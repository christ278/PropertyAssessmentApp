using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using PropertyAssessmentApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PropertyAssessmentApp.Services;

public class RPTCertificatePdfService : IRPTCertificatePdfService
{
    private readonly IWebHostEnvironment? _env;

    public RPTCertificatePdfService(IWebHostEnvironment? env = null)
    {
        _env = env;
    }

    public byte[] GenerateCertificatePdf(OfficialCertificateViewModel model)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;

        byte[]? sealImageBytes = LoadSealImage();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginTop(26);
                page.MarginBottom(20);
                page.MarginHorizontal(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9f).FontColor(Colors.Black));

                if (model.IsNoImprovementCertificate && model.NoImprovementDetails != null)
                {
                    page.Content().Element(c => ComposeNoImprovementContent(c, model.NoImprovementDetails, sealImageBytes));
                    page.Footer().Element(c => ComposeNoImprovementFooter(c, model.NoImprovementDetails));
                }
                else if (model.IsImprovementCertificate && model.ImprovementDetails != null)
                {
                    page.Content().Element(c => ComposeImprovementContent(c, model.ImprovementDetails, sealImageBytes));
                    page.Footer().Element(c => ComposeImprovementFooter(c, model.ImprovementDetails));
                }
                else if (model.IsAssessmentOfTdCertificate && model.AssessmentDetails != null)
                {
                    page.Content().Element(c => ComposeAssessmentOfTdContent(c, model.AssessmentDetails, sealImageBytes));
                    page.Footer().Element(c => ComposeAssessmentOfTdFooter(c, model.AssessmentDetails));
                }
                else if (model.IsNoLandHoldingCertificate && model.NoLandHoldingDetails != null)
                {
                    page.Content().Element(c => ComposeNoLandHoldingContent(c, model.NoLandHoldingDetails, sealImageBytes));
                    page.Footer().Element(c => ComposeNoLandHoldingFooter(c, model.NoLandHoldingDetails));
                }
                else if (model.IsBirCertification && model.BirHoldingsDetails != null)
                {
                    page.Content().Element(c => ComposeBirHoldingsContent(c, model.BirHoldingsDetails, sealImageBytes));
                    page.Footer().Element(c => ComposeBirHoldingsFooter(c, model.BirHoldingsDetails));
                }
                else if (model.IsDarCertification && model.DarDetails != null)
                {
                    page.Content().Element(c => ComposeDarContent(c, model.DarDetails, sealImageBytes));
                    page.Footer().Element(c => ComposeDarFooter(c, model.DarDetails));
                }
                else
                {
                    page.Content().Element(c => ComposeContent(c, model, sealImageBytes));
                    page.Footer().Element(c => ComposeFooter(c, model.Footer));
                }
            });
        });

        return document.GeneratePdf();
    }

    private byte[]? LoadSealImage()
    {
        try
        {
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var candidatePaths = new[]
            {
                Path.Combine(webRoot, "images", "koronadal_seal.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "koronadal_seal.png"),
                Path.Combine(webRoot, "images", "koronadal_seal.jpg"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "koronadal_seal.jpg")
            };

            foreach (var p in candidatePaths)
            {
                if (File.Exists(p))
                {
                    return File.ReadAllBytes(p);
                }
            }
        }
        catch
        {
            // Fallback gracefully to vector emblem
        }

        return null;
    }

    private static void ComposeAssessorBanner(ColumnDescriptor col, string officeTitle, byte[]? sealImageBytes)
    {
        col.Item().PaddingTop(4).PaddingBottom(8).Row(r =>
        {
            if (sealImageBytes != null && sealImageBytes.Length > 0)
            {
                r.ConstantItem(48).Height(48).AlignMiddle().AlignCenter().Image(sealImageBytes);
            }
            else
            {
                r.ConstantItem(48).Height(48).AlignMiddle().AlignCenter().Column(sc =>
                {
                    sc.Item().AlignCenter().Text("CITY OF").FontSize(5f).Bold().FontColor(Color.FromHex("#B91C1C"));
                    sc.Item().AlignCenter().Text("KORONADAL").FontSize(5.5f).ExtraBold().FontColor(Color.FromHex("#B91C1C"));
                    sc.Item().AlignCenter().Text("OFFICIAL SEAL").FontSize(4.2f).Bold().FontColor(Color.FromHex("#D97706"));
                });
            }

            r.RelativeItem().AlignMiddle().Column(c =>
            {
                c.Item().AlignCenter().Text(officeTitle).FontSize(14.5f).ExtraBold().FontColor(Color.FromHex("#B91C1C")).LetterSpacing(0.04f);
                c.Item().PaddingTop(2).AlignCenter().Width(280).Height(1.5f).Background(Color.FromHex("#B91C1C"));
            });

            // Symmetric dummy item on the right to ensure RelativeItem is mathematically centered horizontally on the page
            r.ConstantItem(48).Height(48);
        });
    }

    // =========================================================================
    // 1. SPECIFIC LAYOUT: CERTIFICATE OF NO IMPROVEMENT (MATCHING PHYSICAL LGU SAMPLE)
    // =========================================================================
    private static void ComposeNoImprovementContent(IContainer container, CertificateNoImprovementModel m, byte[]? sealImageBytes)
    {
        container.Column(col =>
        {
            // 1. Top Government Header
            col.Item().AlignCenter().Text(m.HeaderCountry).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderProvince).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderLguName).FontSize(9.5f).Bold().FontColor(Colors.Black);

            // 2. Banner Bar with Seal and OFFICE OF THE CITY ASSESSOR (Centered)
            ComposeAssessorBanner(col, m.HeaderOfficeTitle, sealImageBytes);

            // 3. Document Title
            col.Item().PaddingTop(12).AlignCenter().Text(m.DocumentTitle).FontSize(14f).ExtraBold().FontColor(Colors.Black);

            // 4. Salutation
            col.Item().PaddingTop(14).Text(m.Salutation).FontSize(10f).Bold().FontColor(Colors.Black);

            // 5. Narrative Body Paragraph
            col.Item().PaddingTop(10).Text(t =>
            {
                t.Span("        THIS IS TO CERTIFY that as shown by the records of this office ");
                t.Span(m.OwnerName).Bold();
                t.Span(" , is the registered owner of an/a ");
                t.Span(m.PropertyKindOrUse).Bold();
                t.Span(" land identified as Lot  ");
                t.Span(m.LotNo).Bold();
                t.Span(" , ");
                t.Span(m.AreaDisplay).Bold();
                t.Span(" situated at   ");
                t.Span(m.LocationDisplay).Bold();
                t.Span(", and said property is declared for taxation purposes under ARP NO. ");
                t.Span(m.ArpNo).Bold();
                t.Span(" of this city.");
            });

            // 6. Secondary Certification Sentence
            col.Item().PaddingTop(8).Text(t =>
            {
                t.Span("        This certifies further that the said  Lot ");
                t.Span(m.LotNo).Bold();
                t.Span("  has no structural improvement existing thereon as per tax mapping record.");
            });

            // 7. Disclaimer Note
            col.Item().PaddingTop(10).Text("Note: Any discrepancy between the actual location Tax Maps and Technical Description is subject to correction.").FontSize(8f);

            // 8. Requestor Block
            col.Item().PaddingTop(14).Text("This certification is issued Upon the request of").FontSize(9f).Bold();
            col.Item().PaddingTop(4).Column(rc =>
            {
                // Name & TIN row
                rc.Item().Row(r =>
                {
                    r.AutoItem().PaddingRight(4).Text("Name").FontSize(8.5f);
                    r.RelativeItem(5).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(m.RequestorName) ? " " : m.RequestorName).FontSize(8.5f).Bold();
                    r.ConstantItem(12);
                    r.AutoItem().PaddingRight(4).Text("TIN").FontSize(8.5f);
                    r.RelativeItem(3).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(m.RequestorTin) ? " " : m.RequestorTin).FontSize(8.5f);
                });

                // Address row
                rc.Item().PaddingTop(4).Row(r =>
                {
                    r.AutoItem().PaddingRight(4).Text("Address").FontSize(8.5f);
                    r.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(m.RequestorAddress) ? " " : m.RequestorAddress).FontSize(8.5f);
                });

                // Purpose row
                rc.Item().PaddingTop(4).Row(r =>
                {
                    r.AutoItem().PaddingRight(4).Text("Purpose").FontSize(8.5f);
                    r.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(m.Purpose) ? " " : m.Purpose).FontSize(8.5f);
                });
            });

            // 9. Done date clause
            col.Item().PaddingTop(14).Text($"Done this {m.IssueDay} day of {m.IssueMonth}, '{m.IssueYear} at the {m.IssueLocation}.").FontSize(9f);

            // 10. Dual / Triple Official Signatories
            col.Item().PaddingTop(20).Row(r =>
            {
                // Left Column: Verified by Assessment Clerk III
                r.RelativeItem(5).Column(leftCol =>
                {
                    leftCol.Item().Text(m.ClerkSignatory.SectionTitle).FontSize(8.5f);
                    leftCol.Item().Height(26);
                    leftCol.Item().Text(m.ClerkSignatory.Name).FontSize(8.5f).Bold().Underline();
                    leftCol.Item().Text(m.ClerkSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });

                r.ConstantItem(20);

                // Right Column: Noted by City Assessor & Acting Records Officer
                r.RelativeItem(6).Column(rightCol =>
                {
                    rightCol.Item().Text(m.AssessorSignatory.SectionTitle).FontSize(8.5f);
                    rightCol.Item().Height(24);
                    rightCol.Item().Text(m.AssessorSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.AssessorSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);

                    rightCol.Item().Height(14);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.SectionTitle).FontSize(7.5f).Italic();
                    rightCol.Item().Height(5);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });
            });

            // 11. Fee & Official Receipt (O.R.) Metadata (Aligned Right as in sample)
            col.Item().PaddingTop(16).Row(r =>
            {
                r.RelativeItem();
                r.AutoItem().Width(180).Column(fc =>
                {
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("CERT . FEE").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text($"P{m.CertFee:N2}").FontSize(7.5f).Bold();
                    });
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("O.R NO").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text(m.ORNumber).FontSize(7.5f).Bold();
                    });
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("DATE").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text(m.ORDate).FontSize(7.5f);
                    });
                    fc.Item().PaddingTop(4).Text(m.DocStampFeeDisplay).FontSize(7f).Italic();
                });
            });
        });
    }

    private static void ComposeNoImprovementFooter(IContainer container, CertificateNoImprovementModel m)
    {
        container.Row(r =>
        {
            // Bottom Left: Barcode / Control Sequence Number
            r.RelativeItem().AlignLeft().Text(m.ControlNumber).FontSize(10f).Bold();

            // Bottom Right: Printed By Operator
            r.AutoItem().AlignRight().Text($"Printed By: {m.PrintedBy}").FontSize(7.5f).Bold();
        });
    }

    // =========================================================================
    // 2. SPECIFIC LAYOUT: CERTIFICATE OF IMPROVEMENT (MATCHING PHYSICAL LGU SAMPLE)
    // =========================================================================
    private static void ComposeImprovementContent(IContainer container, CertificateImprovementModel m, byte[]? sealImageBytes)
    {
        container.Column(col =>
        {
            // 1. Top Government Header
            col.Item().AlignCenter().Text(m.HeaderCountry).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderProvince).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderLguName).FontSize(9.5f).Bold().FontColor(Colors.Black);

            // 2. Banner Bar with Seal and OFFICE OF THE CITY ASSESSOR (Centered)
            ComposeAssessorBanner(col, m.HeaderOfficeTitle, sealImageBytes);

            // 3. Document Title
            col.Item().PaddingTop(10).AlignCenter().Text(m.DocumentTitle).FontSize(14f).ExtraBold().FontColor(Colors.Black);

            // 4. Salutation
            col.Item().PaddingTop(12).Text(m.Salutation).FontSize(10f).Bold().FontColor(Colors.Black);

            // 5. First Narrative Paragraph
            col.Item().PaddingTop(8).Text(t =>
            {
                t.Span("        THIS IS TO CERTIFY that as shown  by the records of this office, ");
                t.Span(m.OwnerName).Bold();
                t.Span(" is/are the registered owner/s of a /an ");
                t.Span(m.PropertyKindOrUse);
                t.Span(" land at ");
                t.Span(m.LocationDisplay).Bold();
                t.Span(", identified as Lot No. ");
                t.Span(m.LotNo).Bold();
                t.Span(" ");
                t.Span(m.BlockNo).Bold();
                t.Span(" with an area of ");
                t.Span(m.AreaDisplay).Bold();
                t.Span(" and declared for taxation purposes under Tax Declaration No./ARP No. ");
                t.Span(m.ArpNo).Bold();
                t.Span(" of this city.");
            });

            // 6. Second Narrative Sentence
            col.Item().PaddingTop(6).Text(t =>
            {
                t.Span("        This certifies further that as per actual inspection made by this office on the above mentioned lot, there are structural improvements thereon introduced for taxation purposes, to wit:");
            });

            // 7. Structural Improvements Table
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.8f); // ARP No.
                    columns.RelativeColumn(4.8f); // Name of Owner
                    columns.RelativeColumn(1.3f); // Actual Use
                    columns.RelativeColumn(1.3f); // Kind
                    columns.RelativeColumn(2.0f); // Area
                    columns.RelativeColumn(2.5f); // Market Value
                    columns.RelativeColumn(2.3f); // Assessed Value
                    columns.RelativeColumn(1.8f); // Year Effectivity
                });

                table.Header(header =>
                {
                    static IContainer HeaderStyle(IContainer container) =>
                        container.Border(0.5f).BorderColor(Colors.Black).Background(Color.FromHex("#F1F5F9")).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().AlignMiddle();

                    header.Cell().Element(HeaderStyle).Text("ARP No.").FontSize(7.5f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Name of Owner").FontSize(7.5f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Actual\nUse").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Kind").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Area").FontSize(7.5f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Market\nValue").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Assessed\nValue").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Year\nEffectivity").FontSize(7f).Bold();
                });

                if (m.Improvements != null)
                {
                    foreach (var item in m.Improvements)
                    {
                        static IContainer BodyStyle(IContainer container) =>
                            container.Border(0.5f).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(2).AlignMiddle();

                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.ArpNo).FontSize(7.5f);
                        table.Cell().Element(BodyStyle).AlignLeft().Text(item.OwnerName).FontSize(7.5f).Bold();
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.ActualUse).FontSize(7f);
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.Kind).FontSize(7f).Bold();
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.Area).FontSize(7f);
                        table.Cell().Element(BodyStyle).AlignRight().Text($"{item.MarketValue:N2}").FontSize(7f);
                        table.Cell().Element(BodyStyle).AlignRight().Text($"{item.AssessedValue:N2}").FontSize(7f);
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.YearEffectivity).FontSize(7.5f);
                    }
                }
            });

            // 8. Requestor Block
            col.Item().PaddingTop(10).Text("This certification is issued Upon the request of").FontSize(9f).Bold();
            col.Item().PaddingTop(3).Column(rc =>
            {
                rc.Item().Row(r =>
                {
                    r.AutoItem().PaddingRight(4).Text("Name").FontSize(8.5f);
                    r.RelativeItem(5).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(m.RequestorName) ? " " : m.RequestorName).FontSize(8.5f).Bold();
                    r.ConstantItem(12);
                    r.AutoItem().PaddingRight(4).Text("TIN").FontSize(8.5f);
                    r.RelativeItem(3).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(m.RequestorTin) ? " " : m.RequestorTin).FontSize(8.5f);
                });

                rc.Item().PaddingTop(3).Row(r =>
                {
                    r.AutoItem().PaddingRight(4).Text("Address").FontSize(8.5f);
                    r.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(m.RequestorAddress) ? " " : m.RequestorAddress).FontSize(8.5f);
                });

                rc.Item().PaddingTop(3).Row(r =>
                {
                    r.AutoItem().PaddingRight(4).Text("Purpose").FontSize(8.5f);
                    r.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(m.Purpose) ? " " : m.Purpose).FontSize(8.5f);
                });
            });

            // 9. Done date clause
            col.Item().PaddingTop(10).Text($"Done this {m.IssueDay} day of {m.IssueMonth}, '{m.IssueYear} at the {m.IssueLocation}.").FontSize(9f);

            // 10. Dual / Triple Official Signatories
            col.Item().PaddingTop(16).Row(r =>
            {
                // Left Column: Verified by Assessment Clerk III
                r.RelativeItem(5).Column(leftCol =>
                {
                    leftCol.Item().Text(m.ClerkSignatory.SectionTitle).FontSize(8.5f);
                    leftCol.Item().Height(24);
                    leftCol.Item().Text(m.ClerkSignatory.Name).FontSize(8.5f).Bold().Underline();
                    leftCol.Item().Text(m.ClerkSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });

                r.ConstantItem(20);

                // Right Column: Noted by City Assessor & Acting Records Officer
                r.RelativeItem(6).Column(rightCol =>
                {
                    rightCol.Item().Text(m.AssessorSignatory.SectionTitle).FontSize(8.5f);
                    rightCol.Item().Height(22);
                    rightCol.Item().Text(m.AssessorSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.AssessorSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);

                    rightCol.Item().Height(12);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.SectionTitle).FontSize(7.5f).Italic();
                    rightCol.Item().Height(4);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });
            });

            // 11. Fee & Official Receipt (O.R.) Metadata (Aligned Right as in sample)
            col.Item().PaddingTop(12).Row(r =>
            {
                r.RelativeItem();
                r.AutoItem().Width(180).Column(fc =>
                {
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("CERT . FEE").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text($"P{m.CertFee:N2}").FontSize(7.5f).Bold();
                    });
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("O.R NO").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text(m.ORNumber).FontSize(7.5f).Bold();
                    });
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("DATE").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text(m.ORDate).FontSize(7.5f);
                    });
                    fc.Item().PaddingTop(3).Text(m.DocStampFeeDisplay).FontSize(7f).Italic();
                });
            });
        });
    }

    private static void ComposeImprovementFooter(IContainer container, CertificateImprovementModel m)
    {
        container.Row(r =>
        {
            r.RelativeItem().AlignLeft().Text(m.ControlNumber).FontSize(10f).Bold();
            r.AutoItem().AlignRight().Text($"Printed By: {m.PrintedBy}").FontSize(7.5f).Bold();
        });
    }

    // =========================================================================
    // 3. SPECIFIC LAYOUT: CERTIFICATION OF NO LAND HOLDING (MATCHING PHYSICAL LGU SAMPLE)
    // =========================================================================
    private static void ComposeNoLandHoldingContent(IContainer container, CertificationNoLandHoldingModel m, byte[]? sealImageBytes)
    {
        container.Column(col =>
        {
            // 1. Top Government Header
            col.Item().AlignCenter().Text(m.HeaderCountry).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderProvince).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderLguName).FontSize(9.5f).Bold().FontColor(Colors.Black);

            // 2. Banner Bar with Seal and OFFICE OF THE CITY ASSESSOR (Centered)
            ComposeAssessorBanner(col, m.HeaderOfficeTitle, sealImageBytes);

            // 3. Document Title
            col.Item().PaddingTop(14).AlignCenter().Text(m.DocumentTitle).FontSize(14f).Bold().FontColor(Colors.Black);

            // 4. Salutation
            col.Item().PaddingTop(16).Text(m.Salutation).FontSize(10f).Bold().FontColor(Colors.Black);

            // 5. Narrative Body Paragraph
            col.Item().PaddingTop(12).Text(t =>
            {
                t.Span("        THIS IS TO CERTIFY that as shown by the records of this office, ");
                t.Span(m.TaxpayerName).Bold().Underline();
                t.Span(" resident of ");
                t.Span(m.TaxpayerAddress).Bold().Underline();
                t.Span(" have no real property/ies assessed in his/her/their name/s for taxation purposes in this City .");
            });

            // 6. Requestor Block
            col.Item().PaddingTop(22).Text("This certification is issued upon the request of .").FontSize(9f);
            col.Item().PaddingTop(6).Column(rc =>
            {
                // Name & TIN row
                rc.Item().Row(r =>
                {
                    r.ConstantItem(65).Text("Name").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem(5).Text(string.IsNullOrWhiteSpace(m.RequestorName) ? " " : m.RequestorName).FontSize(8.5f).Bold();
                    r.ConstantItem(12);
                    r.AutoItem().PaddingRight(4).Text("TIN").FontSize(8.5f);
                    r.RelativeItem(3).Text(string.IsNullOrWhiteSpace(m.RequestorTin) ? " " : m.RequestorTin).FontSize(8.5f);
                });

                // Address row
                rc.Item().PaddingTop(5).Row(r =>
                {
                    r.ConstantItem(65).Text("Address").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem().Text(string.IsNullOrWhiteSpace(m.RequestorAddress) ? " " : m.RequestorAddress).FontSize(8.5f);
                });

                // Purpose row
                rc.Item().PaddingTop(5).Row(r =>
                {
                    r.ConstantItem(65).Text("Purpose").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem().Text(string.IsNullOrWhiteSpace(m.Purpose) ? " " : m.Purpose).FontSize(8.5f);
                });
            });

            // 7. Done date clause
            col.Item().PaddingTop(18).Text($"        Done this {m.IssueDay} day of {m.IssueMonth}, '{m.IssueYear} at the {m.IssueLocation}.").FontSize(9f);

            // 8. Dual / Triple Official Signatories
            col.Item().PaddingTop(26).Row(r =>
            {
                // Left Column: Verified as per records
                r.RelativeItem(5).Column(leftCol =>
                {
                    leftCol.Item().Text(m.ClerkSignatory.SectionTitle).FontSize(8.5f);
                    leftCol.Item().Height(28);
                    leftCol.Item().Text(m.ClerkSignatory.Name).FontSize(8.5f).Bold().Underline();
                    leftCol.Item().Text(m.ClerkSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });

                r.ConstantItem(20);

                // Right Column: Noted By City Assessor & Acting Records Officer
                r.RelativeItem(6).Column(rightCol =>
                {
                    rightCol.Item().Text(m.AssessorSignatory.SectionTitle).FontSize(8.5f);
                    rightCol.Item().Height(26);
                    rightCol.Item().Text(m.AssessorSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.AssessorSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);

                    rightCol.Item().Height(14);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.SectionTitle).FontSize(7.5f).Italic();
                    rightCol.Item().Height(6);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });
            });

            // 9. Fee & Official Receipt (O.R.) Metadata (Aligned Right as in sample)
            col.Item().PaddingTop(26).Row(r =>
            {
                r.RelativeItem();
                r.AutoItem().Width(180).Column(fc =>
                {
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("CERT FEE").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text($"P{m.CertFee:N2}").FontSize(7.5f).Bold();
                    });
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("O.R. NO").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text(m.ORNumber).FontSize(7.5f).Bold();
                    });
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("DATE").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text(m.ORDate).FontSize(7.5f);
                    });
                    fc.Item().PaddingTop(3).Text(m.DocStampFeeDisplay).FontSize(7f).Italic();
                });
            });
        });
    }

    private static void ComposeNoLandHoldingFooter(IContainer container, CertificationNoLandHoldingModel m)
    {
        container.Row(r =>
        {
            r.RelativeItem().AlignLeft().Text(m.ControlNumber).FontSize(10f).Bold();
            r.AutoItem().AlignCenter().Text($"Printed By: {m.PrintedBy}").FontSize(7.5f).Bold();
            r.RelativeItem().AlignRight().Text(m.PageDisplay).FontSize(7.5f);
        });
    }

    // =========================================================================
    // 4. SPECIFIC LAYOUT: ALL PROPERTY HOLDINGS (BIR CERTIFICATION)
    // =========================================================================
    private static void ComposeBirHoldingsContent(IContainer container, AllPropertyHoldingsBirModel m, byte[]? sealImageBytes)
    {
        container.Column(col =>
        {
            // 1. Top Government Header
            col.Item().AlignCenter().Text(m.HeaderCountry).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderProvince).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderLguName).FontSize(9.5f).Bold().FontColor(Colors.Black);

            // 2. Banner Bar with Seal and OFFICE OF THE CITY ASSESSOR (Centered)
            ComposeAssessorBanner(col, m.HeaderOfficeTitle, sealImageBytes);

            // 3. Document Title
            col.Item().PaddingTop(10).AlignCenter().Text(m.DocumentTitle).FontSize(14f).Bold().FontColor(Colors.Black);

            // 4. Salutation
            col.Item().PaddingTop(12).Text(m.Salutation).FontSize(10f).Bold().FontColor(Colors.Black);

            // 5. Narrative Body Paragraph
            col.Item().PaddingTop(8).Text(t =>
            {
                t.Span("        THIS IS TO CERTIFY that according to the records of this office, this/these is/are the total REAL PROPERTIES / LANDHOLDING registered in the name/joint names of ");
                t.Span(m.OwnerNames).Bold();
                t.Span(" within the City of Koronadal, Province of South Cotabato.");
            });

            // 6. Property Holdings Table (9 Columns)
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.6f); // TDN
                    columns.RelativeColumn(1.6f); // Kind
                    columns.RelativeColumn(2.8f); // Location
                    columns.RelativeColumn(2.6f); // Lot Number
                    columns.RelativeColumn(2.2f); // Area
                    columns.RelativeColumn(2.5f); // Title Number
                    columns.RelativeColumn(2.4f); // Market Value
                    columns.RelativeColumn(2.3f); // Assessed Value
                    columns.RelativeColumn(1.6f); // Year COMM.
                });

                table.Header(header =>
                {
                    static IContainer HeaderStyle(IContainer container) =>
                        container.Border(0.5f).BorderColor(Colors.Black).Background(Color.FromHex("#F1F5F9")).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().AlignMiddle();

                    header.Cell().Element(HeaderStyle).Text("TDN").FontSize(7.5f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Kind").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Location").FontSize(7.5f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Lot Number").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Area").FontSize(7.5f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Title\nNumber").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Market\nValue").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Assessed\nValue").FontSize(7f).Bold();
                    header.Cell().Element(HeaderStyle).Text("Year\nCOMM.").FontSize(7f).Bold();
                });

                if (m.Holdings != null)
                {
                    foreach (var item in m.Holdings)
                    {
                        static IContainer BodyStyle(IContainer container) =>
                            container.Border(0.5f).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(2).AlignMiddle();

                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.Tdn).FontSize(7.5f);
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.Kind).FontSize(7f).Bold();
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.Location).FontSize(7f).Bold();
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.LotNumber).FontSize(7f);
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.Area).FontSize(7f);
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.TitleNumber).FontSize(6.5f);
                        table.Cell().Element(BodyStyle).AlignRight().Text($"{item.MarketValue:N2}").FontSize(7f);
                        table.Cell().Element(BodyStyle).AlignRight().Text($"{item.AssessedValue:N2}").FontSize(7f);
                        table.Cell().Element(BodyStyle).AlignCenter().Text(item.YearComm).FontSize(7.5f);
                    }
                }
            });

            // 7. Requestor Block
            col.Item().PaddingTop(10).Text("This certification is issued upon the request of").FontSize(9f);
            col.Item().PaddingTop(4).Column(rc =>
            {
                // Name & TIN row
                rc.Item().Row(r =>
                {
                    r.ConstantItem(65).Text("Name").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem(5).Text(string.IsNullOrWhiteSpace(m.RequestorName) ? " " : m.RequestorName).FontSize(8.5f).Bold();
                    r.ConstantItem(12);
                    r.AutoItem().PaddingRight(4).Text("TIN").FontSize(8.5f);
                    r.RelativeItem(3).Text(string.IsNullOrWhiteSpace(m.RequestorTin) ? " " : m.RequestorTin).FontSize(8.5f);
                });

                // Address row
                rc.Item().PaddingTop(4).Row(r =>
                {
                    r.ConstantItem(65).Text("Address").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem().Text(string.IsNullOrWhiteSpace(m.RequestorAddress) ? " " : m.RequestorAddress).FontSize(8.5f);
                });

                // Purpose row
                rc.Item().PaddingTop(4).Row(r =>
                {
                    r.ConstantItem(65).Text("Purpose").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem().Text(string.IsNullOrWhiteSpace(m.Purpose) ? " " : m.Purpose).FontSize(8.5f);
                });
            });

            // 8. Done date clause
            col.Item().PaddingTop(12).Text($"        Done this {m.IssueDay} day of {m.IssueMonth}, '{m.IssueYear} at the {m.IssueLocation}.").FontSize(9f);

            // 9. Dual / Triple Official Signatories
            col.Item().PaddingTop(18).Row(r =>
            {
                // Left Column: Verified as per records
                r.RelativeItem(5).Column(leftCol =>
                {
                    leftCol.Item().Text(m.ClerkSignatory.SectionTitle).FontSize(8.5f);
                    leftCol.Item().Height(24);
                    leftCol.Item().Text(m.ClerkSignatory.Name).FontSize(8.5f).Bold().Underline();
                    leftCol.Item().Text(m.ClerkSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });

                r.ConstantItem(20);

                // Right Column: Noted By City Assessor & Acting Records Officer
                r.RelativeItem(6).Column(rightCol =>
                {
                    rightCol.Item().Text(m.AssessorSignatory.SectionTitle).FontSize(8.5f);
                    rightCol.Item().Height(22);
                    rightCol.Item().Text(m.AssessorSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.AssessorSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);

                    rightCol.Item().Height(12);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.SectionTitle).FontSize(7.5f).Italic();
                    rightCol.Item().Height(5);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });
            });

            // 10. Fee & Official Receipt (O.R.) Metadata (Aligned Right as in sample)
            col.Item().PaddingTop(14).Row(r =>
            {
                r.RelativeItem();
                r.AutoItem().Width(180).Column(fc =>
                {
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("CERT FEE").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text($"P{m.CertFee:N2}").FontSize(7.5f).Bold();
                    });
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("O.R. NO").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text(m.ORNumber).FontSize(7.5f).Bold();
                    });
                    fc.Item().Row(fr =>
                    {
                        fr.ConstantItem(65).Text("DATE").FontSize(7.5f);
                        fr.ConstantItem(10).Text(":").FontSize(7.5f);
                        fr.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingBottom(1)
                            .Text(m.ORDate).FontSize(7.5f);
                    });
                    fc.Item().PaddingTop(3).Text(m.DocStampFeeDisplay).FontSize(7f).Italic();
                });
            });
        });
    }

    private static void ComposeBirHoldingsFooter(IContainer container, AllPropertyHoldingsBirModel m)
    {
        container.Row(r =>
        {
            r.RelativeItem().AlignLeft().Text(m.ControlNumber).FontSize(10f).Bold();
            r.AutoItem().AlignCenter().Text($"Printed By: {m.PrintedBy}").FontSize(7.5f).Bold();
            r.RelativeItem().AlignRight().Text(m.PageDisplay).FontSize(7.5f);
        });
    }

    // =========================================================================
    // 5. SPECIFIC LAYOUT: AGRICULTURAL LANDS (DAR CERTIFICATION)
    // =========================================================================
    private static void ComposeDarContent(IContainer container, AgriculturalDarCertificationModel m, byte[]? sealImageBytes)
    {
        container.Column(col =>
        {
            // 1. Top Government Header
            col.Item().AlignCenter().Text(m.HeaderCountry).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderProvince).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderLguName).FontSize(9.5f).Bold().FontColor(Colors.Black);

            // 2. Banner Bar with Seal and OFFICE OF THE CITY ASSESSOR (Centered)
            ComposeAssessorBanner(col, m.HeaderOfficeTitle, sealImageBytes);

            // 3. Document Title
            col.Item().PaddingTop(10).AlignCenter().Text(m.DocumentTitle).FontSize(14f).ExtraBold().FontColor(Colors.Black);

            // 4. Salutation
            col.Item().PaddingTop(12).Text(m.Salutation).FontSize(10f).Bold().FontColor(Colors.Black);

            // 5. Lead-in Statement
            col.Item().PaddingTop(8).Text(t =>
            {
                t.Span("        This is to certify that the assessment of Tax Declaration No. ");
                t.Span(m.TaxDeclarationNo).Bold();
                t.Span(" as per record of this Office are the following :");
            });

            // 6. Key-Value Property Particulars
            col.Item().PaddingTop(8).PaddingLeft(40).Column(listCol =>
            {
                void AddRow(string label, string value, bool isBold = true)
                {
                    listCol.Item().PaddingVertical(1.5f).Row(r =>
                    {
                        r.ConstantItem(110).Text(label).FontSize(9f);
                        r.ConstantItem(18).AlignCenter().Text(":").FontSize(9f);
                        var valItem = r.RelativeItem().Text(value).FontSize(9f);
                        if (isBold) valItem.Bold();
                    });
                }

                AddRow("Lot #", m.LotNo);
                AddRow("Name of Owner", m.OwnerName);
                
                // Location line
                listCol.Item().PaddingVertical(1.5f).Row(r =>
                {
                    r.ConstantItem(110).Text("Location").FontSize(9f);
                    r.ConstantItem(18).AlignCenter().Text(":").FontSize(9f);
                    r.RelativeItem().Column(lc =>
                    {
                        lc.Item().Text(m.LocationLine1).FontSize(9f).Bold();
                        lc.Item().Text(m.LocationLine2).FontSize(9f).Bold();
                    });
                });

                AddRow("Title No", m.TitleNo, false);
                AddRow("Survey No", m.SurveyNo);
                AddRow("Classification", m.Classification);
                AddRow("Area", m.AreaDisplay);
                AddRow("Market Value", $"{m.MarketValue:N2}");
                AddRow("Assessed Value", $"{m.AssessedValue:N2}");
            });

            // 7. Requestor Statement
            col.Item().PaddingTop(14).Text(t =>
            {
                t.Span("        This certification is hereby issued upon the request of ");
                t.Span(m.RequestorName).Bold().Underline();
                t.Span(" for ");
                t.Span(m.Purpose).Bold().Underline();
                t.Span(" purposes only.");
            });

            // 8. Issuance Date
            col.Item().PaddingTop(8).Text($"        Issued this {m.IssueDay} day of {m.IssueMonth} {m.IssueYear} at {m.IssueLocation}.").FontSize(9f);

            // 9. Dual / Triple Official Signatories
            col.Item().PaddingTop(18).Row(r =>
            {
                // Left Column: Verified as per records
                r.RelativeItem(5).Column(leftCol =>
                {
                    leftCol.Item().Text(m.ClerkSignatory.SectionTitle).FontSize(8.5f);
                    leftCol.Item().Height(24);
                    leftCol.Item().Text(m.ClerkSignatory.Name).FontSize(8.5f).Bold().Underline();
                    leftCol.Item().Text(m.ClerkSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                    if (!string.IsNullOrWhiteSpace(m.ClerkSignatory.SubNote))
                    {
                        leftCol.Item().Text(m.ClerkSignatory.SubNote).FontSize(7f).FontColor(Colors.Grey.Darken3);
                    }
                });

                r.ConstantItem(20);

                // Right Column: Noted by City Assessor & Acting Records Officer
                r.RelativeItem(6).Column(rightCol =>
                {
                    rightCol.Item().Text(m.AssessorSignatory.SectionTitle).FontSize(8.5f);
                    rightCol.Item().Height(22);
                    rightCol.Item().Text(m.AssessorSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.AssessorSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);

                    rightCol.Item().Height(10);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.SectionTitle).FontSize(7.5f).Italic();
                    rightCol.Item().Height(4);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });
            });

            // 10. Fee & Receipt Metadata (Bottom Left)
            col.Item().PaddingTop(14).Column(fc =>
            {
                fc.Item().Row(fr =>
                {
                    fr.ConstantItem(55).Text("Date:").FontSize(7.5f).Italic();
                    fr.RelativeItem().Text(m.ReceiptDate).FontSize(7.5f).Italic();
                });
                fc.Item().Row(fr =>
                {
                    fr.ConstantItem(55).Text("Amount:").FontSize(7.5f).Italic();
                    fr.RelativeItem().Text($"{m.Amount:N2}").FontSize(7.5f).Italic();
                });
                fc.Item().Row(fr =>
                {
                    fr.ConstantItem(55).Text("Or :").FontSize(7.5f).Italic();
                    fr.RelativeItem().Text(m.ORNumber).FontSize(7.5f).Italic();
                });
            });
        });
    }

    private static void ComposeDarFooter(IContainer container, AgriculturalDarCertificationModel m)
    {
        container.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().AlignMiddle().Height(1f).Background(Color.FromHex("#B91C1C"));
                r.AutoItem().PaddingHorizontal(8).Column(bc =>
                {
                    bc.Item().AlignCenter().Text(m.BrandPrefix).FontSize(5.5f).Bold().FontColor(Color.FromHex("#D97706"));
                    bc.Item().AlignCenter().Text(m.BrandTitle).FontSize(9f).ExtraBold().FontColor(Color.FromHex("#B91C1C")).LetterSpacing(0.05f);
                });
                r.RelativeItem().AlignMiddle().Height(1f).Background(Color.FromHex("#B91C1C"));
            });

            col.Item().PaddingTop(2).Row(cr =>
            {
                cr.RelativeItem().AlignCenter().Text($"Tel: {m.ContactPhone}    |    Email: {m.ContactEmail}    |    Office: {m.ContactAddress}").FontSize(6.5f).FontColor(Colors.Grey.Darken3);
            });
        });
    }

    // =========================================================================
    // 2. SPECIFIC LAYOUT: ASSESSMENT OF TAX DECLARATION (MATCHING PHYSICAL LGU SAMPLE)
    // =========================================================================
    private static void ComposeAssessmentOfTdContent(IContainer container, AssessmentOfTaxDeclarationModel m, byte[]? sealImageBytes)
    {
        container.Column(col =>
        {
            // 1. Top Government Header
            col.Item().AlignCenter().Text(m.HeaderCountry).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderProvince).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(m.HeaderLguName).FontSize(9.5f).Bold().FontColor(Colors.Black);

            // 2. Banner Bar with Seal and OFFICE OF THE CITY ASSESSOR (Centered)
            ComposeAssessorBanner(col, m.HeaderOfficeTitle, sealImageBytes);

            // 3. Document Title
            col.Item().PaddingTop(12).AlignCenter().Text(m.DocumentTitle).FontSize(14f).ExtraBold().FontColor(Colors.Black);

            // 4. Salutation
            col.Item().PaddingTop(14).Text(m.Salutation).FontSize(10f).Bold().FontColor(Colors.Black);

            // 5. Intro statement
            col.Item().PaddingTop(10).Text(t =>
            {
                t.Span("        This is to certify that the assessment of Tax Declaration No. ");
                t.Span(m.TaxDeclarationNo).Bold();
                t.Span(" as per record of this Office are the following :");
            });

            // 6. Property Assessment Key-Value Block
            col.Item().PaddingTop(10).PaddingLeft(40).Column(listCol =>
            {
                void AddRow(string label, string value, bool isBold = true)
                {
                    listCol.Item().PaddingVertical(1.5f).Row(r =>
                    {
                        r.ConstantItem(110).Text(label).FontSize(9f);
                        r.ConstantItem(18).AlignCenter().Text(":").FontSize(9f);
                        var valItem = r.RelativeItem().Text(value).FontSize(9f);
                        if (isBold) valItem.Bold();
                    });
                }

                AddRow("Lot #", m.LotNo);
                AddRow("Name of Owner", m.OwnerName);
                AddRow("Location", m.Location);
                AddRow("Title No", string.IsNullOrWhiteSpace(m.TitleNo) ? "" : m.TitleNo, false);
                AddRow("Survey No", string.IsNullOrWhiteSpace(m.SurveyNo) ? "" : m.SurveyNo, false);
                AddRow("Classification", m.Classification);
                AddRow("Area", m.AreaDisplay);
                AddRow("Market Value", $"{m.MarketValue:N2}");
                AddRow("Assessed Value", $"{m.AssessedValue:N2}");
                AddRow("Ownership", m.Ownership);
            });

            // 7. Request statement
            col.Item().PaddingTop(16).Text(t =>
            {
                t.Span("        This certification is hereby issued upon the request of ");
                if (!string.IsNullOrWhiteSpace(m.RequestorName))
                {
                    t.Span(m.RequestorName).Bold();
                }
                t.Span(" for the purpose of ");
                if (!string.IsNullOrWhiteSpace(m.Purpose))
                {
                    t.Span(m.Purpose).Bold();
                }
                t.Span(" .");
            });

            // 8. Issuance date statement
            col.Item().PaddingTop(10).Text($"        Issued this {m.IssueDay} day of {m.IssueMonth} {m.IssueYear} at {m.IssueLocation}.").FontSize(9f);

            // 9. Dual / Triple Official Signatories
            col.Item().PaddingTop(24).Row(r =>
            {
                // Left Column: Verified by Assessment Clerk III
                r.RelativeItem(5).Column(leftCol =>
                {
                    leftCol.Item().Text(m.ClerkSignatory.SectionTitle).FontSize(8.5f);
                    leftCol.Item().Height(28);
                    leftCol.Item().Text(m.ClerkSignatory.Name).FontSize(8.5f).Bold().Underline();
                    leftCol.Item().Text(m.ClerkSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });

                r.ConstantItem(20);

                // Right Column: Noted by City Assessor & Acting Records Officer
                r.RelativeItem(6).Column(rightCol =>
                {
                    rightCol.Item().Text(m.AssessorSignatory.SectionTitle).FontSize(8.5f);
                    rightCol.Item().Height(26);
                    rightCol.Item().Text(m.AssessorSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.AssessorSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);

                    rightCol.Item().Height(14);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.SectionTitle).FontSize(7.5f).Italic();
                    rightCol.Item().Height(6);
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(m.RecordsOfficerSignatory.Designation).FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });
            });

            // 10. Fee & Receipt Metadata (Bottom Left as in sample)
            col.Item().PaddingTop(20).Column(fc =>
            {
                fc.Item().Row(fr =>
                {
                    fr.ConstantItem(55).Text("OR No.").FontSize(7.5f).Italic();
                    fr.RelativeItem().Text(m.ORNumber).FontSize(7.5f).Italic();
                });
                fc.Item().Row(fr =>
                {
                    fr.ConstantItem(55).Text("Date:").FontSize(7.5f).Italic();
                    fr.RelativeItem().Text(m.ORDate).FontSize(7.5f).Italic();
                });
                fc.Item().Row(fr =>
                {
                    fr.ConstantItem(55).Text("Amount:").FontSize(7.5f).Italic();
                    fr.RelativeItem().Text($"P{m.Amount:N2}").FontSize(7.5f).Italic();
                });
                fc.Item().PaddingTop(4).Text(m.DocStampFeeDisplay).FontSize(7f);
            });
        });
    }

    private static void ComposeAssessmentOfTdFooter(IContainer container, AssessmentOfTaxDeclarationModel m)
    {
        container.Row(r =>
        {
            r.RelativeItem();
        });
    }

    // =========================================================================
    // 2. TABULAR LAYOUT: TAX DECLARATION RECORDS CERTIFICATION & OTHER CERTIFICATES
    // =========================================================================
    private static void ComposeContent(IContainer container, OfficialCertificateViewModel model, byte[]? sealImageBytes)
    {
        container.Column(col =>
        {
            // 1. Top Government Header
            col.Item().AlignCenter().Text(model.Header.Country).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(model.Header.Province).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(model.Header.LguName).FontSize(9.5f).Bold().FontColor(Colors.Black);

            // 2. Banner Bar with Seal and OFFICE OF THE CITY ASSESSOR (Centered)
            ComposeAssessorBanner(col, model.Header.OfficeTitle, sealImageBytes);

            // 3. Document Title (CERTIFICATION)
            col.Item().PaddingTop(8).AlignCenter().Text(model.Header.DocumentTitle).FontSize(13f).ExtraBold().FontColor(Colors.Black);

            // 4. Introductory Statement
            col.Item().PaddingTop(10).PaddingBottom(8).Text(model.Header.IntroStatement).FontSize(9f);

            // 5. Official 5-Column Details Table
            col.Item().PaddingTop(4).PaddingBottom(12).Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(32);       // NO.
                    cols.ConstantColumn(110);      // Tax Declaration No.
                    cols.RelativeColumn(1.4f);     // Owner's Name
                    cols.ConstantColumn(80);       // Lot No.
                    cols.RelativeColumn(1.6f);     // Location
                });

                t.Cell().Border(0.75f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).Padding(4).AlignCenter().AlignMiddle()
                    .Text("NO.").FontSize(8f).Bold();
                t.Cell().Border(0.75f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).Padding(4).AlignCenter().AlignMiddle()
                    .Text("Tax Declaration\nNo.").FontSize(8f).Bold();
                t.Cell().Border(0.75f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).Padding(4).AlignCenter().AlignMiddle()
                    .Text("Owner's Name").FontSize(8f).Bold();
                t.Cell().Border(0.75f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).Padding(4).AlignCenter().AlignMiddle()
                    .Text("Lot No.").FontSize(8f).Bold();
                t.Cell().Border(0.75f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).Padding(4).AlignCenter().AlignMiddle()
                    .Text("Location").FontSize(8f).Bold();

                foreach (var item in model.Details.Items)
                {
                    t.Cell().Border(0.75f).BorderColor(Colors.Black).Padding(4).AlignCenter().AlignMiddle()
                        .Text(item.No.ToString()).FontSize(8.5f);
                    t.Cell().Border(0.75f).BorderColor(Colors.Black).Padding(4).AlignCenter().AlignMiddle()
                        .Text(item.TaxDeclarationNo).FontSize(8.5f);
                    t.Cell().Border(0.75f).BorderColor(Colors.Black).Padding(4).AlignLeft().AlignMiddle()
                        .Text(item.OwnerName).FontSize(8.5f).Bold();
                    t.Cell().Border(0.75f).BorderColor(Colors.Black).Padding(4).AlignCenter().AlignMiddle()
                        .Text(item.LotNo).FontSize(8.5f);
                    t.Cell().Border(0.75f).BorderColor(Colors.Black).Padding(4).AlignLeft().AlignMiddle()
                        .Text(item.Location).FontSize(8f);
                }
            });

            // 6. Requestor Particulars
            col.Item().PaddingTop(6).Text(model.Footer.RequestIntro).FontSize(9f);
            col.Item().PaddingTop(4).Column(reqCol =>
            {
                reqCol.Item().Row(r =>
                {
                    r.ConstantItem(60).Text("Name").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem().Text(model.Footer.RequestorName).FontSize(8.5f).Bold();
                });
                reqCol.Item().Row(r =>
                {
                    r.ConstantItem(60).Text("Address").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem().Text(model.Footer.RequestorAddress).FontSize(8.5f);
                });
                reqCol.Item().Row(r =>
                {
                    r.ConstantItem(60).Text("Purpose").FontSize(8.5f);
                    r.ConstantItem(12).Text(":").FontSize(8.5f);
                    r.RelativeItem().Text(model.Footer.Purpose).FontSize(8.5f);
                });
            });

            // 7. Date of Issuance Clause
            col.Item().PaddingTop(12).Text($"Issued this {model.Footer.IssueDay} day of {model.Footer.IssueMonth} {model.Footer.IssueYear} at the {model.Footer.IssueCityProvince}").FontSize(9f);

            // 8. Dual / Triple Official Signatories
            col.Item().PaddingTop(22).Row(r =>
            {
                r.RelativeItem(5).Column(leftCol =>
                {
                    leftCol.Item().Text(model.Footer.VerifiedBy.SectionTitle).FontSize(8.5f);
                    leftCol.Item().Height(28);
                    leftCol.Item().Text(model.Footer.VerifiedBy.Name).FontSize(8.5f).Bold().Underline();
                    leftCol.Item().Text(model.Footer.VerifiedBy.Designation).FontSize(8f).FontColor(Colors.Grey.Darken3);
                });

                r.ConstantItem(20);

                r.RelativeItem(6).Column(rightCol =>
                {
                    rightCol.Item().Text(model.Footer.NotedBy.SectionTitle).FontSize(8.5f);
                    rightCol.Item().Height(26);
                    rightCol.Item().Text(model.Footer.NotedBy.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(model.Footer.NotedBy.Designation).FontSize(8f).FontColor(Colors.Grey.Darken3);

                    rightCol.Item().Height(14);
                    rightCol.Item().Text(model.Footer.ActingOfficer.SectionTitle).FontSize(7.5f).Italic();
                    rightCol.Item().Height(5);
                    rightCol.Item().Text(model.Footer.ActingOfficer.Name).FontSize(8.5f).Bold().Underline();
                    rightCol.Item().Text(model.Footer.ActingOfficer.Designation).FontSize(8f).FontColor(Colors.Grey.Darken3);
                });
            });

            // 9. Fee & Official Receipt (O.R.) Metadata (Bottom-Left)
            col.Item().PaddingTop(16).Column(metaCol =>
            {
                metaCol.Item().Row(r =>
                {
                    r.ConstantItem(55).Text("Cert. Fee").FontSize(8f);
                    r.ConstantItem(12).Text(":").FontSize(8f);
                    r.RelativeItem().Text(model.Footer.CertFee.ToString("N2")).FontSize(8f);
                });
                metaCol.Item().Row(r =>
                {
                    r.ConstantItem(55).Text("O.R No.").FontSize(8f);
                    r.ConstantItem(12).Text(":").FontSize(8f);
                    r.RelativeItem().Text(model.Footer.ORNumber).FontSize(8f).Underline();
                });
                metaCol.Item().Row(r =>
                {
                    r.ConstantItem(55).Text("Date").FontSize(8f);
                    r.ConstantItem(12).Text(":").FontSize(8f);
                    r.RelativeItem().Text(model.Footer.ORDate).FontSize(8f);
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, ReportFooterModel footer)
    {
        container.Column(fCol =>
        {
            fCol.Item().Row(r =>
            {
                r.RelativeItem().AlignMiddle().Height(1).Background(Color.FromHex("#FDA4AF"));
                r.AutoItem().PaddingHorizontal(8).AlignCenter().Row(bRow =>
                {
                    bRow.AutoItem().Text(footer.BrandPrefix + " ").FontSize(7f).Bold().FontColor(Color.FromHex("#0284C7"));
                    bRow.AutoItem().Text(footer.BrandTitle).FontSize(11f).ExtraBold().FontColor(Color.FromHex("#E11D48")).LetterSpacing(0.08f);
                });
                r.RelativeItem().AlignMiddle().Height(1).Background(Color.FromHex("#FDA4AF"));
            });

            fCol.Item().PaddingTop(4).Row(r =>
            {
                r.AutoItem().Text($"Tel: {footer.ContactPhone}").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                r.RelativeItem().AlignCenter().Text($"Email: {footer.ContactEmail}").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                r.AutoItem().AlignRight().Text($"Office: {footer.ContactAddress}").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
            });
        });
    }
}
