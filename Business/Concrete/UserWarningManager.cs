using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class UserWarningManager : IUserWarningService
{
    private readonly IUserWarningDal _userWarningDal;
    private readonly ILogService _logService;

    public UserWarningManager(IUserWarningDal userWarningDal, ILogService logService)
    {
        _userWarningDal = userWarningDal;
        _logService = logService;
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

        _logService.LogInfo("AdminAction", "RevokeWarning",
            $"Uyarı geri alındı - Warning ID: {warningId}");

        return new SuccessResult("Uyarı geri alındı.");
    }
}
