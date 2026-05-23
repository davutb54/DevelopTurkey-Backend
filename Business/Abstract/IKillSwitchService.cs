using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IKillSwitchService
{
    /// <summary>Mevcut kill switch durumunu döner (yoksa Off olarak başlatır).</summary>
    IDataResult<SystemKillSwitch> GetState();

    /// <summary>Kill switch modunu ayarlar. Mode = Off → deactivate akışıdır.</summary>
    IResult SetMode(byte mode, string? reason, int actorUserId);

    /// <summary>True ise <paramref name="minimumMode"/> veya daha yüksek bir mod aktif demektir.</summary>
    bool IsActive(byte minimumMode = KillSwitchMode.Soft);

    bool IsSoft();
    bool IsHard();
    bool IsEmergency();
}
