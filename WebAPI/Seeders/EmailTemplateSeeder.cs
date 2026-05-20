using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace WebAPI.Seeders;

public static class EmailTemplateSeeder
{
    public static void Seed(DevelopTurkeyContext context)
    {
        if (!context.EmailTemplates.Any())
        {
            var templates = new List<EmailTemplate>
            {
                new EmailTemplate
                {
                    TemplateKey = "EmailVerification",
                    Subject = "Develop Turkey - Email Doğrulama",
                    Body = "<h3>Hoşgeldin {UserName},</h3><p>Hesabını doğrulamak için kodun: <h1 style='color:#3b82f6'>{Code}</h1></p><p>Develop Turkey ekibi.</p>",
                    Description = "Kullanıcı kayıt olduğunda gönderilen doğrulama kodu e-postası.",
                    AvailablePlaceholders = "{UserName}, {Code}",
                    IsActive = true
                },
                new EmailTemplate
                {
                    TemplateKey = "PasswordReset",
                    Subject = "Develop Turkey - Şifre Sıfırlama Talebi",
                    Body = "<h3>Merhaba {UserName},</h3><p>Şifreni sıfırlamak için kullanacağın kod: <h1 style='color:#ef4444'>{Code}</h1></p><p>Bu işlemi sen yapmadıysan lütfen bu e-postayı dikkate alma.</p><p>Develop Turkey ekibi.</p>",
                    Description = "Şifre sıfırlama talebinde bulunulduğunda gönderilen kod e-postası.",
                    AvailablePlaceholders = "{UserName}, {Code}",
                    IsActive = true
                }
            };

            context.EmailTemplates.AddRange(templates);
            context.SaveChanges();
        }
    }
}
