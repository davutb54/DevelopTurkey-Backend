using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace WebAPI.Seeders;

public static class InstitutionSeeder
{
    public const string PublicDomain = "so7le.com";

    public static void Seed(DevelopTurkeyContext context)
    {
        if (context.Institutions.Any(i => i.Domain == PublicDomain))
            return;

        context.Institutions.Add(new Institution
        {
            Name           = "SÖ7LE",
            Subtitle       = "Türkiye'yi Geliştirme Platformu",
            Domain         = PublicDomain,
            PrimaryColor   = "#2563eb",
            Status         = true,
        });

        context.SaveChanges();
    }
}
