using Core.Entities.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IEmailVerificationService
{
    Task<IResult> SendVerificationCode(User user);
    Task<IResult> SendPasswordResetCode(User user);
    IResult Verify(string email, int code);
    IResult VerifyForResetPassword(string email, int code);
}