using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Helpers.Email;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class EmailVerificationManager : IEmailVerificationService
{
    private readonly IEmailVerificationDal _emailVerificationDal;
    private readonly IUserDal _userDal;
    private readonly IEmailHelper _emailHelper;

    public EmailVerificationManager(IEmailVerificationDal emailVerificationDal, IUserDal userDal, IEmailHelper emailHelper)
    {
        _emailVerificationDal = emailVerificationDal;
        _userDal = userDal;
        _emailHelper = emailHelper;
    }

    public IResult SendVerificationCode(User user)
    {
        Random random = new Random();
        int code = random.Next(100000, 999999);

        var verification = new EmailVerification
        {
            UserId = user.Id,
            VerificationCode = code,
            SendDate = DateTime.Now,
            ExpirationDate = DateTime.Now.AddMinutes(15),
            IsVerified = false,
            IsExpired = false
        };

        _emailVerificationDal.Add(verification);

        string subject = "Develop Turkey - Email Doğrulama";
        string body = $"<h3>Hoşgeldin {user.Name},</h3><p>Hesabını doğrulamak için kodun: <h1>{code}</h1></p>";

        var sendResult = _emailHelper.Send(user.Email, subject, body);

        if (!sendResult.Success)
        {
            return new ErrorResult("Kayıt oldu ama mail gidemedi: " + sendResult.Message);
        }

        return new SuccessResult("Doğrulama kodu e-posta adresinize gönderildi.");
    }

    public IResult Verify(string email, int code)
    {
        var user = _userDal.Get(u => u.Email == email);
        if (user == null) return new ErrorResult("Kullanıcı bulunamadı.");

        if (user.IsEmailVerified) return new ErrorResult("Bu hesap zaten doğrulanmış.");

        var verifications = _emailVerificationDal.GetAll(v =>
            v.UserId == user.Id &&
            v.VerificationCode == code &&
            v.IsVerified == false &&
            v.IsExpired == false
        );

        var validVerification = verifications.OrderByDescending(v => v.SendDate).FirstOrDefault();

        if (validVerification == null)
        {
            return new ErrorResult("Geçersiz veya süresi dolmuş kod.");
        }

        if (validVerification.ExpirationDate < DateTime.Now)
        {
            validVerification.IsExpired = true;
            _emailVerificationDal.Update(validVerification);
            return new ErrorResult("Kodun süresi dolmuş.");
        }

        validVerification.IsVerified = true;
        validVerification.VerificationDate = DateTime.Now;
        _emailVerificationDal.Update(validVerification);

        user.IsEmailVerified = true;
        _userDal.Update(user);

        return new SuccessResult("Email başarıyla doğrulandı!");
    }

    public IResult SendPasswordResetCode(User user)
    {
        Random random = new Random();
        int code = random.Next(100000, 999999);

        var verification = new EmailVerification
        {
            UserId = user.Id,
            VerificationCode = code,
            SendDate = DateTime.Now,
            ExpirationDate = DateTime.Now.AddMinutes(15),
            IsVerified = false,
            IsExpired = false
        };

        _emailVerificationDal.Add(verification);

        string subject = "Develop Turkey - Şifre Sıfırlama Talebi";
        string body = $"<h3>Merhaba {user.Name},</h3><p>Şifreni sıfırlamak için kullanacağın kod: <h1 style='color:red'>{code}</h1></p><p>Bu işlemi sen yapmadıysan dikkate alma.</p>";

        var sendResult = _emailHelper.Send(user.Email, subject, body);
        if (!sendResult.Success)
        {
            return new ErrorResult("Mail gönderilemedi: " + sendResult.Message);
        }

        return new SuccessResult("Şifre sıfırlama kodu gönderildi.");
    }
}