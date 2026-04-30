using Core.Entities;

namespace Entities.Concrete;

public class SystemSettings : IEntity
{
    public int Id { get; set; }
    public bool IsMaintenanceMode { get; set; }
    public bool DisableNewRegistrations { get; set; }
    public string? MaintenanceMessage { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }

    // --- Site / Kurum Kimliği ---
    public string? SiteName { get; set; }           // Örn: "Develop Turkey"
    public string? SiteDescription { get; set; }    // Footer'da slogan veya kısa açıklama
    public string? OrganizationName { get; set; }   // Kurum Adı (Footer'daki uzun yazı yerine geçecek alan)

    // --- İletişim Bilgileri ---
    public string? ContactFullName { get; set; }    // Ad Soyad
    public string? ContactAddress { get; set; }     // Açık adres
    public string? ContactEmail { get; set; }       // E-posta
    public string? ContactPhone { get; set; }       // Telefon numarası

    // --- Sosyal Medya ---
    public string? SocialTwitter { get; set; }      // Twitter/X URL
    public string? SocialInstagram { get; set; }    // Instagram URL
    public string? SocialLinkedIn { get; set; }     // LinkedIn URL
}
