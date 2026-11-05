using System;
using PropertyAssessmentApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PropertyAssessmentApp.Services;

public class RPTCertificatePdfService : IRPTCertificatePdfService
{
    public byte[] GenerateCertificatePdf(OfficialCertificateViewModel model)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

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

                page.Content().Element(c => ComposeContent(c, model));
                page.Footer().Element(c => ComposeFooter(c, model.Footer));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeContent(IContainer container, OfficialCertificateViewModel model)
    {
        container.Column(col =>
        {
            // 1. Top Government Header
            col.Item().AlignCenter().Text(model.Header.Country).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(model.Header.Province).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            col.Item().AlignCenter().Text(model.Header.LguName).FontSize(9.5f).Bold().FontColor(Colors.Black);

            // 2. Banner Bar with Seal and OFFICE OF THE CITY ASSESSOR
            col.Item().PaddingTop(4).PaddingBottom(8)
                .Border(1).BorderColor(Color.FromHex("#FECDD3"))
                .Background(Color.FromHex("#FFF1F2"))
                .PaddingVertical(6).PaddingHorizontal(10)
                .Row(r =>
                {
                    // Official City Seal emblem badge
                    r.AutoItem().Width(44).Height(44).Border(1.5f).BorderColor(Color.FromHex("#B91C1C")).Background(Colors.White).Padding(2).AlignCenter().AlignMiddle().Column(sc =>
                    {
                        sc.Item().AlignCenter().Text("CITY OF").FontSize(5f).Bold().FontColor(Color.FromHex("#B91C1C"));
                        sc.Item().AlignCenter().Text("KORONADAL").FontSize(5.5f).ExtraBold().FontColor(Color.FromHex("#B91C1C"));
                        sc.Item().AlignCenter().Text("OFFICIAL SEAL").FontSize(4.2f).Bold().FontColor(Color.FromHex("#D97706"));
                    });

                    // Office of the City Assessor text in deep crimson red
                    r.RelativeItem().PaddingLeft(10).AlignMiddle().Column(c =>
                    {
                        c.Item().AlignCenter().Text(model.Header.OfficeTitle).FontSize(14.5f).ExtraBold().FontColor(Color.FromHex("#B91C1C")).LetterSpacing(0.04f);
                        c.Item().PaddingTop(2).Height(1.5f).Background(Color.FromHex("#B91C1C"));
                    });
                });

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

                // Table Header Cells
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

                // Table Rows
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
                // Left Column: Verified by Assessment Clerk III
                r.RelativeItem(5).Column(leftCol =>
                {
                    leftCol.Item().Text(model.Footer.VerifiedBy.SectionTitle).FontSize(8.5f);
                    leftCol.Item().Height(28);
                    leftCol.Item().Text(model.Footer.VerifiedBy.Name).FontSize(8.5f).Bold().Underline();
                    leftCol.Item().Text(model.Footer.VerifiedBy.Designation).FontSize(8f).FontColor(Colors.Grey.Darken3);
                });

                r.ConstantItem(20);

                // Right Column: Noted by City Assessor & Acting Records Officer
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
            // Horizontal decorative accent with centered LUNGSOD NG KORONADAL
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

            // Contact info strip at the very bottom
            fCol.Item().PaddingTop(4).Row(r =>
            {
                r.AutoItem().Text($"Tel: {footer.ContactPhone}").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                r.RelativeItem().AlignCenter().Text($"Email: {footer.ContactEmail}").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                r.AutoItem().AlignRight().Text($"Office: {footer.ContactAddress}").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
            });
        });
    }
}
