using Business.Abstract;
using Business.Models;
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
    private readonly ILogService _logService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IWorkflowEventBus _eventBus;

    public EmailVerificationManager(
        IEmailVerificationDal emailVerificationDal,
        IUserDal userDal,
        IEmailHelper emailHelper,
        ILogService logService,
        IEmailTemplateService emailTemplateService,
        IWorkflowEventBus eventBus)
    {
        _emailVerificationDal = emailVerificationDal;
        _userDal = userDal;
        _emailHelper = emailHelper;
        _logService = logService;
        _emailTemplateService = emailTemplateService;
        _eventBus = eventBus;
    }

    public async Task<IResult> SendVerificationCode(User user)
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

        // Get template from database
        var templateResult = _emailTemplateService.GetByKey("EmailVerification");
        string subject = "Develop Turkey - Email Doğrulama";
        string body = $"<h3>Hoşgeldin {user.Name},</h3><p>Hesabını doğrulamak için kodun: <h1>{code}</h1></p>";

        if (templateResult.Success)
        {
            var template = templateResult.Data;
            subject = template.Subject;

            var renderResult = _emailTemplateService.RenderTemplate(template.Body, new Dictionary<string, string>
            {
                { "{UserName}", user.Name },
                { "{Code}", code.ToString() }
            });

            if (renderResult.Success)
            {
                body = renderResult.Data;
            }
        }

        var sendResult = await _emailHelper.SendAsync(user.Email, subject, body);

        if (!sendResult.Success)
        {
            _logService.LogError("Auth", "SendVerification", $"Email gönderme hatası - UserID: {user.Id}, Email: {user.Email}", sendResult.Message);
            return new ErrorResult("Kayıt oldu ama mail gidemedi: " + sendResult.Message);
        }

        _ = _eventBus.PublishAsync("auth.verification_code_sent", new RuleContext
        {
            SystemUserId = user.Id
        });

        _logService.LogInfo("Auth", "SendVerification", $"Doğrulama kodu gönderildi - UserID: {user.Id}");
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
            // TODO: Belirli sayıda (Örn: 5) başarısız doğrulama denemesinden sonra ilgili kodu geçersiz kılacak bir sayaç mekanizması eklenmeli mi?
            _logService.LogWarning("Auth", "Verify", $"Geçersiz doğrulama kodu denemesi - UserID: {user.Id}");
            return new ErrorResult("Geçersiz veya süresi dolmuş kod.");
        }

        if (validVerification.ExpirationDate < DateTime.Now)
        {
            validVerification.IsExpired = true;
            _emailVerificationDal.Update(validVerification);

            _logService.LogWarning("Auth", "Verify", $"Süresi dolmuş doğrulama kodu denemesi - UserID: {user.Id}");
            return new ErrorResult("Kodun süresi dolmuş.");
        }

        validVerification.IsVerified = true;
        validVerification.VerificationDate = DateTime.Now;
        _emailVerificationDal.Update(validVerification);

        user.IsEmailVerified = true;
        _userDal.Update(user);

        _logService.LogInfo("Auth", "Verify", $"Email başarıyla doğrulandı - UserID: {user.Id}");

        _ = _eventBus.PublishAsync("auth.email_verified", new RuleContext
        {
            SystemUserId = user.Id
        });

        return new SuccessResult("Email başarıyla doğrulandı!");
    }

    public IResult VerifyForResetPassword(string email, int code)
    {
        var user = _userDal.Get(u => u.Email == email);
        if (user == null) return new ErrorResult("Kullanıcı bulunamadı.");

        var verifications = _emailVerificationDal.GetAll(v =>
            v.UserId == user.Id &&
            v.VerificationCode == code &&
            v.IsVerified == false &&
            v.IsExpired == false
        );

        var validVerification = verifications.OrderByDescending(v => v.SendDate).FirstOrDefault();

        if (validVerification == null)
        {
            // TODO: Belirli sayıda (Örn: 5) başarısız doğrulama denemesinden sonra ilgili kodu geçersiz kılacak bir sayaç mekanizması eklenmeli mi?
            _logService.LogWarning("Auth", "VerifyForReset", $"Geçersiz doğrulama kodu denemesi - UserID: {user.Id}");
            return new ErrorResult("Geçersiz veya süresi dolmuş kod.");
        }

        if (validVerification.ExpirationDate < DateTime.Now)
        {
            validVerification.IsExpired = true;
            _emailVerificationDal.Update(validVerification);

            _logService.LogWarning("Auth", "VerifyForReset", $"Süresi dolmuş doğrulama kodu denemesi - UserID: {user.Id}");
            return new ErrorResult("Kodun süresi dolmuş.");
        }

        validVerification.IsVerified = true;
        validVerification.VerificationDate = DateTime.Now;
        _emailVerificationDal.Update(validVerification);

        user.IsEmailVerified = true;
        _userDal.Update(user);

        _ = _eventBus.PublishAsync("auth.password_reset_verified", new RuleContext
        {
            SystemUserId = user.Id
        });

        _logService.LogInfo("Auth", "VerifyForReset", $"Email başarıyla doğrulandı - UserID: {user.Id}");
        return new SuccessResult("Email başarıyla doğrulandı!");
    }

    public async Task<IResult> SendPasswordResetCode(User user)
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

        // Get template from database
        var templateResult = _emailTemplateService.GetByKey("PasswordReset");
        string subject = "Develop Turkey - Şifre Sıfırlama Talebi";
        string body = $"<h3>Merhaba {user.Name},</h3><p>Şifreni sıfırlamak için kullanacağın kod: <h1 style='color:red'>{code}</h1></p><p>Bu işlemi sen yapmadıysan dikkate alma.</p>";

        if (templateResult.Success)
        {
            var template = templateResult.Data;
            subject = template.Subject;

            var renderResult = _emailTemplateService.RenderTemplate(template.Body, new Dictionary<string, string>
            {
                { "{UserName}", user.Name },
                { "{Code}", code.ToString() }
            });

            if (renderResult.Success)
            {
                body = renderResult.Data;
            }
        }

        var sendResult = await _emailHelper.SendAsync(user.Email, subject, body);
        if (!sendResult.Success)
        {
            _logService.LogError("Auth", "SendPasswordReset", $"Email gönderme hatası - UserID: {user.Id}, Email: {user.Email}", sendResult.Message);
            return new ErrorResult("Mail gönderilemedi: " + sendResult.Message);
        }

        _logService.LogInfo("Auth", "SendPasswordReset", $"Şifre sıfırlama kodu gönderildi - UserID: {user.Id}");

        _ = _eventBus.PublishAsync("auth.password_reset_requested", new RuleContext
        {
            SystemUserId = user.Id
        });

        return new SuccessResult("Şifre sıfırlama kodu gönderildi.");
    }
}
