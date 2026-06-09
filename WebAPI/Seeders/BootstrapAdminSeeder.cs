using Business.Constants;
using Core.Utilities.Security.Hashing;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Core.Entities.Concrete;

namespace WebAPI.Seeders;

public static class BootstrapAdminSeeder
{
    public static void Seed(DevelopTurkeyContext context, IConfiguration config)
    {
        var section = config.GetSection("BootstrapAdmin");
        if (!section.GetValue<bool>("Enabled")) return;

        var email = section["Email"];
        if (string.IsNullOrWhiteSpace(email)) return;

        // Kullanıcı yoksa oluştur (bir kere)
        var user = context.Users.FirstOrDefault(u => u.Email == email && !u.IsDeleted);
        if (user == null)
        {
            HashingHelper.CreatePasswordHash(
                section["DefaultPassword"] ?? "ChangeMeAfterFirstLogin!",
                out byte[] hash, out byte[] salt);

            user = new User
            {
                UserName        = section["Username"] ?? "superadmin",
                Name            = section["Name"] ?? "Bootstrap Admin",
                Surname         = section["Surname"] ?? "Admin",
                Email           = email,
                PasswordHash    = hash,
                PasswordSalt    = salt,
                IsEmailVerified = true,
                RegisterDate    = DateTime.UtcNow,
                InstitutionId   = 1,
            };
            context.Users.Add(user);
            context.SaveChanges();

            context.CapabilityAuditLogs.Add(new CapabilityAuditLog
            {
                ActorUserId  = 0,
                TargetUserId = user.Id,
                Action       = "bootstrap_admin",
                PayloadJson  = $"{{\"email\":\"{email}\"}}",
                CreatedAt    = DateTime.UtcNow,
            });
            context.SaveChanges();
        }

        // Her startup'ta eksik capability'leri grant et.
        // CapabilityDefaults.BootstrapAdmin güncellendiğinde yeni kodlar otomatik eklenir.
        var capCodes = context.Capabilities
            .Where(c => CapabilityDefaults.BootstrapAdmin.Contains(c.Code) && c.IsActive)
            .Select(c => new { c.Id, c.Code })
            .ToList();

        var existingCapIds = context.UserCapabilities
            .Where(uc => uc.UserId == user.Id && uc.Status == 1)
            .Select(uc => uc.CapabilityId)
            .ToHashSet();

        var added = false;
        foreach (var cap in capCodes)
        {
            if (existingCapIds.Contains(cap.Id)) continue;

            context.UserCapabilities.Add(new UserCapability
            {
                UserId        = user.Id,
                CapabilityId  = cap.Id,
                InstitutionId = null,
                Status        = 1,
                GrantedBy     = 0,
                GrantedAt     = DateTime.UtcNow,
                Reason        = "bootstrap_admin",
            });
            added = true;
        }

        if (added) context.SaveChanges();
    }
}
