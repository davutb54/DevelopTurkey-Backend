using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

public class KillSwitchManager : IKillSwitchService
{
    private readonly ISystemKillSwitchDal _dal;
    private readonly ILogger<KillSwitchManager> _logger;

    public KillSwitchManager(ISystemKillSwitchDal dal, ILogger<KillSwitchManager> logger)
    {
        _dal = dal;
        _logger = logger;
    }

    // ── Singleton row erişimi (yoksa Off kaydı oluşturur) ─────────────────────

    private SystemKillSwitch GetOrCreate()
    {
        var existing = _dal.Get(k => k.Id == 1);
        if (existing is not null) return existing;

        var fresh = new SystemKillSwitch
        {
            Id = 1,
            Mode = KillSwitchMode.Off,
            UpdatedAt = DateTime.UtcNow,
        };
        _dal.Add(fresh);
        return fresh;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public IDataResult<SystemKillSwitch> GetState()
    {
        var state = GetOrCreate();
        return new SuccessDataResult<SystemKillSwitch>(state);
    }

    public IResult SetMode(byte mode, string? reason, int actorUserId)
    {
        if (mode > KillSwitchMode.Emergency)
            return new ErrorResult($"Geçersiz mod: {mode}. Geçerli değerler: 0–3.");

        var state = GetOrCreate();
        var previousMode = state.Mode;

        state.Mode = mode;
        state.Reason = reason;
        state.UpdatedAt = DateTime.UtcNow;

        if (mode == KillSwitchMode.Off)
        {
            state.DeactivatedAt = DateTime.UtcNow;
            state.DeactivatedByUserId = actorUserId;
            state.ActivatedAt = null;
            state.ActivatedByUserId = null;
        }
        else
        {
            state.ActivatedAt = DateTime.UtcNow;
            state.ActivatedByUserId = actorUserId;
            state.DeactivatedAt = null;
            state.DeactivatedByUserId = null;
        }

        _dal.Update(state);

        _logger.LogWarning(
            "[KillSwitch] Mode changed {PreviousMode} → {NewMode} by user={ActorUserId}, reason={Reason}",
            previousMode, mode, actorUserId, reason);

        var modeLabel = mode switch
        {
            KillSwitchMode.Off => "devre dışı",
            KillSwitchMode.Soft => "SOFT (yeni run'lar engellendi)",
            KillSwitchMode.Hard => "HARD (worker'lar durdu)",
            KillSwitchMode.Emergency => "EMERGENCY (tüm altyapı donduruldu)",
            _ => mode.ToString(),
        };

        return new SuccessResult($"Kill switch {modeLabel} olarak ayarlandı.");
    }

    // ── Hızlı kontrol — WorkflowOrchestrator ve Consumer'larda çağrılır ────────

    public bool IsActive(byte minimumMode = KillSwitchMode.Soft)
    {
        var state = _dal.Get(k => k.Id == 1);
        return state is not null && state.Mode >= minimumMode;
    }

    public bool IsSoft()      => IsActive(KillSwitchMode.Soft);
    public bool IsHard()      => IsActive(KillSwitchMode.Hard);
    public bool IsEmergency() => IsActive(KillSwitchMode.Emergency);
}
