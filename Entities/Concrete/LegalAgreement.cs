using Core.Entities;

namespace Entities.Concrete;

public class LegalAgreement : IEntity
{
    public int Id { get; set; }
    public string Title { get; set; }           // "Kullanım Koşulları"
    public string Type { get; set; }            // "TermsOfService" | "PrivacyPolicy" | "KVKK"
    public string Version { get; set; }         // "1.0", "2.0"
    public string Content { get; set; }         // Markdown metni
    public bool IsMajorVersion { get; set; }    // true = tüm kullanıcılar yeniden onaylar
    public bool IsActive { get; set; } = false; // Yalnızca bir sürüm aktif olabilir
    public DateTime PublishedAt { get; set; } = DateTime.Now;
    public int PublishedByAdminId { get; set; }
}
