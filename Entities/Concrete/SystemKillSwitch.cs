using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

/// <summary>
/// Kill switch modu sabitleri.
/// </summary>
public static class KillSwitchMode
{
    /// <summary>Normal çalışma.</summary>
    public const byte Off = 0;
    /// <summary>Yeni workflow run'ları reddedilir; devam edenler bitirilir.</summary>
    public const byte Soft = 1;
    /// <summary>Yeni NodeRun işlemleri durdurulur; worker'lar drain eder.</summary>
    public const byte Hard = 2;
    /// <summary>Tüm workflow altyapısı dondurulur.</summary>
    public const byte Emergency = 3;
}

/// <summary>
/// Singleton tablo (her zaman tek satır, Id = 1).
/// Workflow altyapısını acil durumda durdurmak için kullanılır.
/// </summary>
[Index(nameof(Mode), Name = "IX_SystemKillSwitch_Mode")]
public class SystemKillSwitch : IEntity
{
    /// <summary>Her zaman 1 — singleton satır.</summary>
    public int Id { get; set; }

    /// <summary>0=Off, 1=Soft, 2=Hard, 3=Emergency — <see cref="KillSwitchMode"/> sabitlerine bakın.</summary>
    public byte Mode { get; set; } = KillSwitchMode.Off;

    [MaxLength(500)]
    public string? Reason { get; set; }

    public int? ActivatedByUserId { get; set; }
    public DateTime? ActivatedAt { get; set; }

    public int? DeactivatedByUserId { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>Her güncelleme sonrası otomatik set edilir.</summary>
    public DateTime UpdatedAt { get; set; }
}
