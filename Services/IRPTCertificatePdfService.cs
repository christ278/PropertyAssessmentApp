using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IRPTCertificatePdfService
{
    /// <summary>
    /// Generates the official LGU City Assessor Certificate PDF (QuestPDF)
    /// replicating the physical Koronadal City Assessor certificate template.
    /// </summary>
    byte[] GenerateCertificatePdf(OfficialCertificateViewModel model);
}
