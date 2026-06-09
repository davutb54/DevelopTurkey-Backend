using Core.Utilities.Context;
using DataAccess.Concrete.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.HostedServices;

/// <summary>
/// Günlük çalışan orphan medya temizleyici.
/// wwwroot/uploads altındaki dosyaları DB referanslarıyla karşılaştırır;
/// referanssız dosyaları _quarantine klasörüne taşır, QuarantineDays geçince siler.
/// </summary>
public sealed class MediaCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MediaCleanupHostedService> _logger;

    // Temizlik yapılmayacak alt dizinler
    private static readonly string[] KnownUploadDirs = ["problems", "solutions", "profiles", "institutions", "topics"];

    public MediaCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration,
        ILogger<MediaCleanupHostedService> logger)
    {
        _scopeFactory    = scopeFactory;
        _webHostEnvironment = webHostEnvironment;
        _configuration  = configuration;
        _logger         = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk çalıştırmayı yapılandırılmış saate ertele (varsayılan 03:00)
        var firstRun = NextRunTime();
        _logger.LogInformation("MediaCleanup ilk çalışma zamanı: {NextRun:O}", firstRun);
        await Task.Delay(firstRun - DateTime.Now, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MediaCleanup sırasında hata oluştu");
            }

            // Bir sonraki gün için bekle
            await Task.Delay(NextRunTime() - DateTime.Now, stoppingToken);
        }
    }

    private DateTime NextRunTime()
    {
        int hour = _configuration.GetValue("MediaCleanup:RunAtHour", 3);
        var candidate = DateTime.Today.AddHours(hour);
        if (candidate <= DateTime.Now) candidate = candidate.AddDays(1);
        return candidate;
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_webHostEnvironment.WebRootPath)) return;

        int quarantineDays = _configuration.GetValue("MediaCleanup:QuarantineDays", 7);
        string uploadsRoot  = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        string quarantineRoot = Path.Combine(uploadsRoot, "_quarantine");

        if (!Directory.Exists(uploadsRoot)) return;

        // Tenant filtresini bypass et — tüm kurumların dosyalarını görelim
        TenantScopeContext.Set(null, isGlobalAdmin: true);

        Dictionary<string, HashSet<string>> liveRefs;
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DevelopTurkeyContext>();
            liveRefs = await GatherLiveRefsAsync(context, ct);
        }

        int quarantined = 0;
        int deleted     = 0;

        foreach (var dir in KnownUploadDirs)
        {
            string dirPath = Path.Combine(uploadsRoot, dir);
            if (!Directory.Exists(dirPath)) continue;

            string qDir = Path.Combine(quarantineRoot, dir);
            Directory.CreateDirectory(qDir);

            liveRefs.TryGetValue(dir, out var refs);
            refs ??= [];

            foreach (var filePath in Directory.EnumerateFiles(dirPath))
            {
                ct.ThrowIfCancellationRequested();
                var fi       = new FileInfo(filePath);
                var fileName = fi.Name;

                // Çok yeni dosyaları (< 2 saat) atla — upload yarıda kalmış olabilir
                if (fi.CreationTimeUtc > DateTime.UtcNow.AddHours(-2)) continue;

                if (refs.Contains(fileName)) continue; // canlı referans var

                // Karantinaya taşı
                string dest = Path.Combine(qDir, fileName);
                if (!File.Exists(dest))
                {
                    File.Move(filePath, dest);
                    quarantined++;
                    _logger.LogInformation("Karantina: {Dir}/{File}", dir, fileName);
                }
            }
        }

        // Karantina süresini dolduran dosyaları sil
        foreach (var dir in KnownUploadDirs)
        {
            string qDir = Path.Combine(quarantineRoot, dir);
            if (!Directory.Exists(qDir)) continue;

            foreach (var filePath in Directory.EnumerateFiles(qDir))
            {
                var fi = new FileInfo(filePath);
                if (fi.CreationTimeUtc < DateTime.UtcNow.AddDays(-quarantineDays))
                {
                    File.Delete(filePath);
                    deleted++;
                    _logger.LogInformation("Silindi (karantina süresi doldu): {File}", fi.Name);
                }
            }
        }

        _logger.LogInformation(
            "MediaCleanup tamamlandı — {Quarantined} karantina, {Deleted} silme.",
            quarantined, deleted);
    }

    private static async Task<Dictionary<string, HashSet<string>>> GatherLiveRefsAsync(
        DevelopTurkeyContext context, CancellationToken ct)
    {
        var refs = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["problems"]     = new(StringComparer.OrdinalIgnoreCase),
            ["solutions"]    = new(StringComparer.OrdinalIgnoreCase),
            ["profiles"]     = new(StringComparer.OrdinalIgnoreCase),
            ["institutions"] = new(StringComparer.OrdinalIgnoreCase),
            ["topics"]       = new(StringComparer.OrdinalIgnoreCase),
        };

        // Problem görselleri (CSV)
        var problemCsvs = await context.Problems
            .Where(p => p.ImageUrls != null)
            .Select(p => p.ImageUrls!)
            .ToListAsync(ct);
        foreach (var csv in problemCsvs)
            foreach (var f in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                refs["problems"].Add(f.Trim());

        // Solution görselleri (CSV)
        var solutionCsvs = await context.Solutions
            .Where(s => s.ImageUrls != null)
            .Select(s => s.ImageUrls!)
            .ToListAsync(ct);
        foreach (var csv in solutionCsvs)
            foreach (var f in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                refs["solutions"].Add(f.Trim());

        // Profil fotoğrafları
        var profiles = await context.Users
            .Where(u => u.ProfileImageUrl != null)
            .Select(u => u.ProfileImageUrl!)
            .ToListAsync(ct);
        foreach (var f in profiles)
            refs["profiles"].Add(f.Trim());

        // Kurum logoları ("/uploads/institutions/guid.jpg" → "guid.jpg")
        var logos = await context.Institutions
            .Where(i => i.LogoUrl != null)
            .Select(i => i.LogoUrl!)
            .ToListAsync(ct);
        foreach (var path in logos)
            refs["institutions"].Add(Path.GetFileName(path.Trim()));

        // Kategori görselleri
        var topicImages = await context.Topics
            .Where(t => t.ImageName != null)
            .Select(t => t.ImageName)
            .ToListAsync(ct);
        foreach (var f in topicImages)
            refs["topics"].Add(f.Trim());

        // MediaAsset tablosu (videolar + ilerideki görsel referansları)
        var assets = await context.MediaAssets
            .Where(m => !m.IsDeleted)
            .Select(m => new { m.OwnerType, m.FileName })
            .ToListAsync(ct);
        foreach (var a in assets)
        {
            // "Problem" → "problems", "Solution" → "solutions"
            var dir = a.OwnerType.ToLowerInvariant() + "s";
            if (refs.TryGetValue(dir, out var set))
                set.Add(a.FileName);
        }

        return refs;
    }
}
