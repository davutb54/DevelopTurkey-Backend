using Business.Constants;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Core.Entities.Concrete;

namespace WebAPI.Seeders;

/// <summary>
/// Workflow engine'in tetikleyici kullanıcısını oluşturur.
/// Bu hesap login'e kapalıdır (PasswordHash = null), sadece internal workflow çalıştırmak için kullanılır.
/// </summary>
public static class SystemUserSeeder
{
    public const string SystemUserEmail = "system@developturkey.internal";

    public static void Seed(DevelopTurkeyContext context, IConfiguration config)
    {
        // Idempotency: audit action daha önce çalıştıysa atla
        if (context.CapabilityAuditLogs.Any(a => a.Action == "bootstrap_system_user")) return;

        var user = context.Users.FirstOrDefault(u => u.Email == SystemUserEmail && !u.IsDeleted);
        if (user == null)
        {
            user = new User
            {
                UserName    = "system",
                Name        = "System",
                Surname     = "User",
                Email       = SystemUserEmail,
                PasswordHash = null,   // Login'e kapalı
                PasswordSalt = null,
                IsEmailVerified = true,
                RegisterDate = DateTime.UtcNow,
                InstitutionId = 1,
            };
            context.Users.Add(user);
            context.SaveChanges();
        }

        // System user için workflow capability'lerini grant et
        var capCodes = context.Capabilities
            .Where(c => CapabilityDefaults.SystemUser.Contains(c.Code) && c.IsActive)
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
                UserId       = user.Id,
                CapabilityId = cap.Id,
                InstitutionId = null,
                Status       = 1,
                GrantedBy    = 0,
                GrantedAt    = DateTime.UtcNow,
                Reason       = "bootstrap_system_user",
            });
            added = true;
        }

        if (added) context.SaveChanges();

        // System user ID'sini config'e yaz (appsettings üzerinden okunabilir)
        context.CapabilityAuditLogs.Add(new CapabilityAuditLog
        {
            ActorUserId   = 0,
            TargetUserId  = user.Id,
            Action        = "bootstrap_system_user",
            PayloadJson   = $"{{\"userId\":{user.Id},\"email\":\"{SystemUserEmail}\",\"capCount\":{capCodes.Count}}}",
            CreatedAt     = DateTime.UtcNow,
        });
        context.SaveChanges();
    }
}
