namespace Entities.DTOs;

public class CreateAgreementDto
{
    public string Title { get; set; }
    public string Type { get; set; }            // "TermsOfService" | "PrivacyPolicy" | "KVKK"
    public string Version { get; set; }         // "1.0", "2.0"
    public string Content { get; set; }         // Markdown metni
    public bool IsMajorVersion { get; set; }
}
