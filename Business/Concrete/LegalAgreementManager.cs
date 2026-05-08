using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class LegalAgreementManager : ILegalAgreementService
{
    private readonly ILegalAgreementDal _legalAgreementDal;
    private readonly IUserAgreementAcceptanceDal _acceptanceDal;
    private readonly ILogService _logService;
    private readonly IUserDal _userDal;

    public LegalAgreementManager(
        ILegalAgreementDal legalAgreementDal,
        IUserAgreementAcceptanceDal acceptanceDal,
        ILogService logService,
        IUserDal userDal)
    {
        _legalAgreementDal = legalAgreementDal;
        _acceptanceDal = acceptanceDal;
        _logService = logService;
        _userDal = userDal;
    }

    // ─── Herkese açık ────────────────────────────────────────────────────────

    public IDataResult<List<LegalAgreement>> GetActiveAgreements()
    {
        var agreements = _legalAgreementDal.GetAll(a => a.IsActive);
        return new SuccessDataResult<List<LegalAgreement>>(agreements);
    }

    // ─── Giriş yapmış kullanıcılar ───────────────────────────────────────────

    public IDataResult<bool> HasPendingMajorAgreement(int userId)
    {
        // Tüm aktif major sözleşmeleri çek
        var majorAgreements = _legalAgreementDal.GetAll(a => a.IsActive && a.IsMajorVersion);

        if (!majorAgreements.Any())
            return new SuccessDataResult<bool>(false);

        // Kullanıcının kabul ettiği sözleşme ID'lerini çek
        var acceptedIds = _acceptanceDal
            .GetAll(a => a.UserId == userId)
            .Select(a => a.AgreementId)
            .ToHashSet();

        // Herhangi bir major sözleşme kabul edilmemişse → bekliyor
        bool hasPending = majorAgreements.Any(a => !acceptedIds.Contains(a.Id));
        return new SuccessDataResult<bool>(hasPending);
    }

    public IResult Accept(int userId, int agreementId, string? ipAddress)
    {
        var agreement = _legalAgreementDal.Get(a => a.Id == agreementId);
        if (agreement == null)
            return new ErrorResult("Sözleşme bulunamadı.");

        // İdempotent: zaten kabul ettiyse tekrar kayıt açma
        var existing = _acceptanceDal.Get(a => a.UserId == userId && a.AgreementId == agreementId);
        if (existing != null)
            return new SuccessResult("Sözleşme zaten kabul edilmiş.");

        var acceptance = new UserAgreementAcceptance
        {
            UserId = userId,
            AgreementId = agreementId,
            AgreementVersion = agreement.Version,
            AcceptedAt = DateTime.Now,
            IpAddress = ipAddress
        };

        _acceptanceDal.Add(acceptance);

        _logService.LogInfo(
            "Legal",
            "Accept",
            $"Kullanıcı (ID:{userId}) sözleşmeyi kabul etti. AgreementId:{agreementId}, Version:{agreement.Version}",
            $"IP: {ipAddress ?? "bilinmiyor"}");

        return new SuccessResult("Sözleşme başarıyla kabul edildi.");
    }

    // ─── Admin — Yönetim ─────────────────────────────────────────────────────

    public IDataResult<List<LegalAgreement>> GetAll()
    {
        var agreements = _legalAgreementDal.GetAll();
        return new SuccessDataResult<List<LegalAgreement>>(agreements);
    }

    public IDataResult<LegalAgreement> GetById(int id)
    {
        var agreement = _legalAgreementDal.Get(a => a.Id == id);
        if (agreement == null)
            return new ErrorDataResult<LegalAgreement>(null, "Sözleşme bulunamadı.");

        return new SuccessDataResult<LegalAgreement>(agreement);
    }

    public IResult Create(CreateAgreementDto dto, int adminId)
    {
        // Aynı Type + Version kombinasyonu zaten varsa hata dön
        var duplicate = _legalAgreementDal.Get(a => a.Type == dto.Type && a.Version == dto.Version);
        if (duplicate != null)
            return new ErrorResult($"'{dto.Type}' tipinde '{dto.Version}' versiyonu zaten mevcut.");

        var agreement = new LegalAgreement
        {
            Title = dto.Title,
            Type = dto.Type,
            Version = dto.Version,
            Content = dto.Content,
            IsMajorVersion = dto.IsMajorVersion,
            IsActive = false,
            PublishedAt = DateTime.Now,
            PublishedByAdminId = adminId
        };

        _legalAgreementDal.Add(agreement);

        _logService.LogInfo(
            "AdminAction",
            "AgreementCreate",
            $"Yeni sözleşme oluşturuldu. Tip:{dto.Type}, Versiyon:{dto.Version}, AdminId:{adminId}",
            $"MajorVersion:{dto.IsMajorVersion}");

        return new SuccessResult("Sözleşme başarıyla oluşturuldu.");
    }

    public IResult Activate(int id)
    {
        var target = _legalAgreementDal.Get(a => a.Id == id);
        if (target == null)
            return new ErrorResult("Sözleşme bulunamadı.");

        // Aynı Type'taki diğer aktif sözleşmeleri pasife al (atomik — aynı context üzerinden)
        var sameTypeActive = _legalAgreementDal.GetAll(a => a.Type == target.Type && a.IsActive && a.Id != id);
        foreach (var old in sameTypeActive)
        {
            old.IsActive = false;
            _legalAgreementDal.Update(old);
        }

        target.IsActive = true;
        _legalAgreementDal.Update(target);

        _logService.LogInfo(
            "AdminAction",
            "AgreementActivate",
            $"Sözleşme aktifleştirildi. Id:{id}, Tip:{target.Type}, Versiyon:{target.Version}",
            $"Pasife alınan önceki versiyon sayısı: {sameTypeActive.Count}");

        return new SuccessResult("Sözleşme aktifleştirildi.");
    }

    public IResult Delete(int id)
    {
        var agreement = _legalAgreementDal.Get(a => a.Id == id);
        if (agreement == null)
            return new ErrorResult("Sözleşme bulunamadı.");

        if (agreement.IsActive)
            return new ErrorResult("Aktif sözleşme silinemez. Önce başka bir versiyonu aktifleştirin.");

        _legalAgreementDal.Delete(agreement);

        _logService.LogInfo(
            "AdminAction",
            "AgreementDelete",
            $"Sözleşme silindi. Id:{id}, Tip:{agreement.Type}, Versiyon:{agreement.Version}");

        return new SuccessResult("Sözleşme silindi.");
    }

    // ─── Kabul istatistikleri (Admin) ─────────────────────────────────────────

    public IDataResult<int> GetAcceptanceCount(int agreementId)
    {
        var count = _acceptanceDal.GetAll(a => a.AgreementId == agreementId).Count;
        return new SuccessDataResult<int>(count);
    }

    public IDataResult<double> GetAcceptanceRate(int agreementId)
    {
        var totalActiveUsers = _userDal.GetAll(u => !u.IsBanned).Count;
        if (totalActiveUsers == 0)
            return new SuccessDataResult<double>(0.0);

        var acceptedCount = _acceptanceDal.GetAll(a => a.AgreementId == agreementId).Count;
        var rate = Math.Round((double)acceptedCount / totalActiveUsers * 100, 2);
        return new SuccessDataResult<double>(rate);
    }
}
