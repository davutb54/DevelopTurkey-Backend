using Business.Abstract;
using Business.Models;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class UserWarningManager : IUserWarningService
{
    private readonly IUserWarningDal _userWarningDal;
    private readonly ILogService _logService;
    private readonly IWorkflowEventBus _eventBus;

    public UserWarningManager(IUserWarningDal userWarningDal, ILogService logService, IWorkflowEventBus eventBus)
    {
        _userWarningDal = userWarningDal;
        _logService = logService;
        _eventBus = eventBus;
    }

    public IDataResult<List<UserWarning>> GetByUserId(int userId)
    {
        var warnings = _userWarningDal.GetAll(w => w.UserId == userId)
                                      .OrderByDescending(w => w.IssuedAt)
                                      .ToList();
        return new SuccessDataResult<List<UserWarning>>(warnings, "Uyarılar listelendi.");
    }

    public IDataResult<int> GetActiveWarningCount(int userId)
    {
        var count = _userWarningDal.Count(w => w.UserId == userId && w.IsActive);
        return new SuccessDataResult<int>(count, "Aktif uyarı sayısı.");
    }

    public IResult Issue(UserWarning warning)
    {
        warning.IssuedAt = DateTime.Now;
        warning.IsActive = true;
        _userWarningDal.Add(warning);

        _ = _eventBus.PublishAsync("user.warning_issued", new RuleContext
        {
            SystemUserId = warning.UserId,
            Metadata = new Dictionary<string, object?>
            {
                ["WarningId"] = warning.Id,
                ["Severity"] = warning.Severity,
                ["Title"] = warning.Title
            }
        });

        _logService.LogWarning("AdminAction", "IssueWarning",
            $"Uyarı verildi - Kullanıcı ID: {warning.UserId}, Seviye: {warning.Severity}");

        return new SuccessResult("Uyarı başarıyla verildi.");
    }

    public IResult Revoke(int warningId)
    {
        var warning = _userWarningDal.Get(w => w.Id == warningId);
        if (warning == null)
            return new ErrorResult("Uyarı bulunamadı.");

        warning.IsActive = false;
        _userWarningDal.Update(warning);

        _ = _eventBus.PublishAsync("user.warning_revoked", new RuleContext
        {
            SystemUserId = warning.UserId,
            Metadata = new Dictionary<string, object?>
            {
                ["WarningId"] = warningId
            }
        });

        _logService.LogInfo("AdminAction", "RevokeWarning",
            $"Uyarı geri alındı - Warning ID: {warningId}");

        return new SuccessResult("Uyarı geri alındı.");
    }
}
