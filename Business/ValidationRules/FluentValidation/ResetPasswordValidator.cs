using Entities.DTOs.User;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");
        RuleFor(r => r.Code)
            .GreaterThan(0).WithMessage("Geçersiz doğrulama kodu.");
        RuleFor(r => r.NewPassword).StrongPassword();
    }
}
