using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Seeders;

public static class FeatureSeeder
{
    public static void Seed(DevelopTurkeyContext context)
    {
        // Grupları idempotent olarak ekle (yoksa ekle, varsa atla)
        var groupDefinitions = new (string Name, int OrderIndex)[]
        {
            ("Kimlik ve Erişim", 1),
            ("İçerik Oluşturma", 2),
            ("Sosyal Etkileşim", 3),
            ("Moderasyon ve Uzmanlık", 4),
            ("Bildirim ve İletişim", 5),
            ("Hukuki ve Kurumsal", 6),
            ("Veri ve Raporlama", 7),
            ("Güvenlik ve Denetim", 8),
            ("Performans ve UX", 9),
            ("Profil ve Gizlilik", 10),
        };

        foreach (var (name, order) in groupDefinitions)
        {
            if (!context.FeatureGroups.Any(g => g.Name == name))
            {
                context.FeatureGroups.Add(new FeatureGroup { Name = name, OrderIndex = order });
            }
        }
        context.SaveChanges();

        // Grup ID'lerini al
        var identity = context.FeatureGroups.First(g => g.Name == "Kimlik ve Erişim");
        var content = context.FeatureGroups.First(g => g.Name == "İçerik Oluşturma");
        var social = context.FeatureGroups.First(g => g.Name == "Sosyal Etkileşim");
        var moderation = context.FeatureGroups.First(g => g.Name == "Moderasyon ve Uzmanlık");
        var communication = context.FeatureGroups.First(g => g.Name == "Bildirim ve İletişim");
        var security = context.FeatureGroups.First(g => g.Name == "Güvenlik ve Denetim");
        var ux = context.FeatureGroups.First(g => g.Name == "Performans ve UX");
        var profile = context.FeatureGroups.First(g => g.Name == "Profil ve Gizlilik");

        // Tanımları idempotent olarak ekle (Key'e göre kontrol)
        var definitions = new List<FeatureDefinition>
        {
            // --- Kimlik ve Erişim ---
            new() { GroupId = identity.Id, Key = "Identity.AllowGoogleLogin", DisplayName = "Google ile Giriş", Description = "Kullanıcıların Google OAuth 2.0 ile giriş yapmasına izin verir.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 1 },
            new() { GroupId = identity.Id, Key = "Identity.RequireEmailVerification", DisplayName = "Zorunlu E-Posta Doğrulaması", Description = "Kayıt sonrası e-posta doğrulaması zorunlu olur.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 2 },
            new() { GroupId = identity.Id, Key = "Identity.EnableCaptcha", DisplayName = "Bot Koruması (Captcha)", Description = "Giriş ve kayıt ekranlarında Cloudflare Turnstile koruması.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 3 },
            new() { GroupId = identity.Id, Key = "Identity.AllowImpersonation", DisplayName = "Yönetici Sudo Geçişi", Description = "Yöneticilerin başka kullanıcı hesabına şifresiz geçiş yapabilmesi.", InputType = "Boolean", DefaultValue = "false", OrderIndex = 4, IsSystemLevel = true },
            new() { GroupId = identity.Id, Key = "Identity.SessionTimeoutMinutes", DisplayName = "Oturum Zaman Aşımı (Dakika)", Description = "Kullanıcı oturumunun kaç dakika sonra zaman aşımına uğrayacağı.", InputType = "Number", DefaultValue = "60", OrderIndex = 5 },
            new() { GroupId = identity.Id, Key = "Identity.MaxLoginAttempts", DisplayName = "Maksimum Giriş Denemesi", Description = "Hesap kilitlenmeden önce izin verilen maksimum başarısız giriş sayısı.", InputType = "Number", DefaultValue = "5", OrderIndex = 6 },

            // --- İçerik Oluşturma ---
            new() { GroupId = content.Id, Key = "Content.EnableMapLocation", DisplayName = "Harita Özelliği Aktif", Description = "Sorun bildirirken harita üzerinden konum seçimi özelliğini açar.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 1 },
            new() { GroupId = content.Id, Key = "Content.RequireMapLocation", DisplayName = "Harita Konumu Zorunlu", Description = "Harita aktifken, konum seçimi yapılmadan sorun bildirilmesine izin vermez.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 2 },
            new() { GroupId = content.Id, Key = "Content.AllowImageUpload", DisplayName = "Sorun Görsel Yükleme", Description = "Sorunlara, kategorilere ve profile görsel yükleme yeteneği.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 3 },
            new() { GroupId = content.Id, Key = "Content.AllowSolutionImageUpload", DisplayName = "Çözüm Görsel Yükleme", Description = "Çözümlere görsel yükleme yeteneği.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 4 },
            new() { GroupId = content.Id, Key = "Content.MaxProblemImageCount", DisplayName = "Maksimum Sorun Görsel Sayısı", Description = "Bir soruna eklenebilecek maksimum görsel sayısı.", InputType = "Number", DefaultValue = "5", OrderIndex = 5 },
            new() { GroupId = content.Id, Key = "Content.MaxSolutionImageCount", DisplayName = "Maksimum Çözüm Görsel Sayısı", Description = "Bir çözüme eklenebilecek maksimum görsel sayısı.", InputType = "Number", DefaultValue = "3", OrderIndex = 6 },
            new() { GroupId = content.Id, Key = "Content.MaxTitleLength", DisplayName = "Başlık Karakter Limiti", Description = "Sorun başlığı için maksimum karakter sayısı.", InputType = "Number", DefaultValue = "200", OrderIndex = 7 },
            new() { GroupId = content.Id, Key = "Content.RequireCategorySelection", DisplayName = "Zorunlu Kategori Seçimi", Description = "Sorun girerken kategori seçimi zorunlu olur.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 8 },
            new() { GroupId = content.Id, Key = "Content.MinSolutionLength", DisplayName = "Minimum Çözüm Uzunluğu", Description = "Kaliteli içerik için çözüm metninin minimum karakter sayısı.", InputType = "Number", DefaultValue = "50", OrderIndex = 9 },
            new() { GroupId = content.Id, Key = "Content.EnableCustomHierarchy", DisplayName = "Özel Hiyerarşi Aktif", Description = "Şehir seçimi yerine kurumun tanımladığı hiyerarşik yapıyı kullanır.", InputType = "Boolean", DefaultValue = "false", OrderIndex = 10 },
            new() { GroupId = content.Id, Key = "Content.RequireLocationSelection", DisplayName = "Bölge/Hiyerarşi Seçimi Zorunlu", Description = "Sorun eklerken Şehir veya Özel Hiyerarşi seçimini zorunlu tutar.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 11 },
            new() { GroupId = content.Id, Key = "Content.EnableInstantSolution", DisplayName = "Sorun Anında Çözüm Ekleme", Description = "Sorun oluşturulurken aynı ekranda çözüm önerisi eklenmesine izin verir.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 12 },

            // --- Sosyal Etkileşim ---
            new() { GroupId = social.Id, Key = "Social.EnableUpvote", DisplayName = "İçerik Oylama Sistemi", Description = "Sorunlar için 'Ben de Yaşıyorum' ve çözümler için oy sistemi.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 1 },
            new() { GroupId = social.Id, Key = "Social.EnableNestedComments", DisplayName = "Çoklu Seviye Yorum Sistemi", Description = "Çözümlerin altına yorum yapma ve yorumlara yanıt verme.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 2 },
            new() { GroupId = social.Id, Key = "Social.EnableFollowSystem", DisplayName = "Takip Sistemi", Description = "Kategorileri veya sorunları takip etme özelliği.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 3 },
            new() { GroupId = social.Id, Key = "Social.EnableSavedSolutions", DisplayName = "Çözüm Kaydetme", Description = "Kullanıcıların çözümleri kaydedebilmesi.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 4 },
            new() { GroupId = social.Id, Key = "Social.EnableMentions", DisplayName = "Kullanıcı Etiketleme (@mention)", Description = "Kullanıcıların birbirlerini etiketlemesine izin verir.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 5 },
            new() { GroupId = social.Id, Key = "Social.EnableSharing", DisplayName = "Sosyal Medya Paylaşımı", Description = "Sorunların ve çözümlerin WhatsApp, Twitter, Instagram gibi platformlarda paylaşılmasını sağlar.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 6 },

            // --- Moderasyon ve Uzmanlık ---
            new() { GroupId = moderation.Id, Key = "Moderation.RequireExpertApproval", DisplayName = "Uzman Onay Sistemi", Description = "Çözümlerin yayınlanmadan önce uzman onayından geçmesi.", InputType = "Boolean", DefaultValue = "false", OrderIndex = 1 },
            new() { GroupId = moderation.Id, Key = "Moderation.EnableReportSystem", DisplayName = "Şikayet Mekanizması", Description = "Kullanıcıların içerikleri ve diğer kullanıcıları şikayet edebilmesi.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 3 },

            // --- Bildirim ve İletişim ---
            new() { GroupId = communication.Id, Key = "Communication.EnableSignalR", DisplayName = "Canlı Bildirimler (SignalR)", Description = "Anlık bildirim sistemi. Kapalıysa klasik sayfa yenilemeli mod.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 1 },
            new() { GroupId = communication.Id, Key = "Communication.EnableFeedbackInbox", DisplayName = "Geri Bildirim Kutusu", Description = "Kullanıcıların yönetime doğrudan mesaj atabilmesi.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 2 },

            // --- Güvenlik ve Denetim ---
            new() { GroupId = security.Id, Key = "Identity.EnableIpWhitelist", DisplayName = "IP Beyaz Liste (Whitelist)", Description = "Sadece belirli IP aralıklarından erişime izin verme.", InputType = "Boolean", DefaultValue = "false", OrderIndex = 2, IsSystemLevel = true },
            new() { GroupId = security.Id, Key = "Identity.EnableIpBlacklist", DisplayName = "IP Kara Liste (Blacklist)", Description = "Belirli IP adreslerinin sisteme erişimini engelleme.", InputType = "Boolean", DefaultValue = "false", OrderIndex = 3, IsSystemLevel = true },

            // --- Performans ve UX ---
            new() { GroupId = ux.Id, Key = "UX.InfiniteScrollEnabled", DisplayName = "Sonsuz Kaydırma", Description = "Listelerde sayfalama yerine infinite scroll kullanımı. Kapalıysa klasik sayfalama kullanılır.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 1 },

            // --- Profil ve Gizlilik ---
            new() { GroupId = profile.Id, Key = "Profile.AllowPrivacySettings", DisplayName = "Gizlilik Ayarlarına İzin Ver", Description = "Kullanıcıların kendi profil gizlilik ayarlarını yönetmesine izin verir.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 1 },
            new() { GroupId = profile.Id, Key = "Profile.ShowJoinedDate", DisplayName = "Katılım Tarihini Göster", Description = "Kullanıcı profillerinde kayıt tarihinin gösterilip gösterilmeyeceği.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 2 },
            new() { GroupId = profile.Id, Key = "Profile.ShowStatistics", DisplayName = "İstatistikleri Göster", Description = "Profilde toplam sorun ve çözüm sayılarının gösterilip gösterilmeyeceği.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 3 },
            new() { GroupId = profile.Id, Key = "Profile.AllowUsernameChange", DisplayName = "Kullanıcı Adı Değişimine İzin Ver", Description = "Kullanıcıların kendi kullanıcı adlarını değiştirmesine izin verir.", InputType = "Boolean", DefaultValue = "true", OrderIndex = 4 },
        };

        foreach (var def in definitions)
        {
            if (!context.FeatureDefinitions.Any(d => d.Key == def.Key))
            {
                context.FeatureDefinitions.Add(def);
            }
        }
        context.SaveChanges();
    }
}
