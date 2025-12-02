using Core.Entities.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IEmailVerificationService
{
    IResult SendVerificationCode(User user);

    IResult Verify(string email, int code);
    IResult SendPasswordResetCode(User user);
}