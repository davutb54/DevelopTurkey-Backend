using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface ILegalAgreementService
{
    // Herkese açık (AllowAnonymous)
    IDataResult<List<LegalAgreement>> GetActiveAgreements(); // Kayıt ekranı için

    // Giriş yapmış kullanıcılar
    IDataResult<bool> HasPendingMajorAgreement(int userId);                     // AuthContext kontrolü
    IResult Accept(int userId, int agreementId, string? ipAddress);             // Onay kaydet

    // Admin — Yönetim
    IDataResult<List<LegalAgreement>> GetAll();     // Tüm versiyon geçmişi
    IDataResult<LegalAgreement> GetById(int id);
    IResult Create(CreateAgreementDto dto, int adminId);
    IResult Activate(int id);                       // Bu versiyonu aktif yap
    IResult Delete(int id);                         // Sadece aktif olmayan silinebilir

    // Kabul istatistikleri (Admin)
    IDataResult<int> GetAcceptanceCount(int agreementId);       // Kaç kullanıcı onayladı
    IDataResult<double> GetAcceptanceRate(int agreementId);     // Aktif kullanıcılara oranı
}
