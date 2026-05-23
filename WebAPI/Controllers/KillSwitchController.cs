using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

/// <summary>
/// Workflow kill switch yönetimi.
/// Soft/Hard/Emergency modları arasında geçiş yapılır.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class KillSwitchController : ControllerBase
{
    private readonly IKillSwitchService _killSwitch;
    private readonly Core.Utilities.Authorization.ICapabilityPolicy _policy;

    public KillSwitchController(
        IKillSwitchService killSwitch,
        Core.Utilities.Authorization.ICapabilityPolicy policy)
    {
        _killSwitch = killSwitch;
        _policy = policy;
    }

    private int CurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ── GET /api/killswitch ────────────────────────────────────────────────────

    /// <summary>Kill switch'in mevcut durumunu döner.</summary>
    [HttpGet]
    [RequireCapability("admin.killswitch_read")]
    public IActionResult GetState()
    {
        var result = _killSwitch.GetState();
        if (!result.Success) return BadRequest(result);

        var state = result.Data!;
        return Ok(new
        {
            success = true,
            data = new
            {
                mode = state.Mode,
                modeLabel = ModeLabel(state.Mode),
                isActive = state.Mode > KillSwitchMode.Off,
                reason = state.Reason,
                activatedAt = state.ActivatedAt,
                activatedByUserId = state.ActivatedByUserId,
                deactivatedAt = state.DeactivatedAt,
                updatedAt = state.UpdatedAt,
            }
        });
    }

    // ── POST /api/killswitch/soft ─────────────────────────────────────────────

    /// <summary>Soft mod: yeni workflow run'larını engeller, devam edenler tamamlanır.</summary>
    [HttpPost("soft")]
    [RequireCapability("admin.killswitch_soft")]
    public IActionResult SetSoft([FromBody] KillSwitchRequestDto dto)
    {
        var result = _killSwitch.SetMode(KillSwitchMode.Soft, dto.Reason, CurrentUserId());
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    // ── POST /api/killswitch/hard ─────────────────────────────────────────────

    /// <summary>Hard mod: worker'ları durdurur, yeni NodeRun işlemleri kabul edilmez.</summary>
    [HttpPost("hard")]
    [RequireCapability("admin.killswitch_hard")]
    public IActionResult SetHard([FromBody] KillSwitchRequestDto dto)
    {
        var result = _killSwitch.SetMode(KillSwitchMode.Hard, dto.Reason, CurrentUserId());
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    // ── POST /api/killswitch/emergency ────────────────────────────────────────

    /// <summary>Emergency mod: tüm workflow altyapısını dondurur. Geri alınabilir.</summary>
    [HttpPost("emergency")]
    [RequireCapability("admin.killswitch_emergency")]
    public IActionResult SetEmergency([FromBody] KillSwitchRequestDto dto)
    {
        var result = _killSwitch.SetMode(KillSwitchMode.Emergency, dto.Reason, CurrentUserId());
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    // ── POST /api/killswitch/deactivate ──────────────────────────────────────

    /// <summary>Kill switch'i tamamen kapatır (Off moda alır).</summary>
    [HttpPost("deactivate")]
    [RequireCapability("admin.killswitch_soft")]   // En az soft yetkisi yeterli
    public IActionResult Deactivate([FromBody] KillSwitchRequestDto dto)
    {
        var result = _killSwitch.SetMode(KillSwitchMode.Off, dto.Reason, CurrentUserId());
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    // ── Yardımcı ──────────────────────────────────────────────────────────────

    private static string ModeLabel(byte mode) => mode switch
    {
        KillSwitchMode.Off       => "Off",
        KillSwitchMode.Soft      => "Soft",
        KillSwitchMode.Hard      => "Hard",
        KillSwitchMode.Emergency => "Emergency",
        _                        => $"Unknown({mode})",
    };
}

/// <summary>Soft / Hard / Emergency / Deactivate endpoint'leri için istek gövdesi.</summary>
public sealed class KillSwitchRequestDto
{
    public string? Reason { get; set; }
}
