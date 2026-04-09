using Entities.DTOs.User;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation;

public class UserPasswordUpdateValidator : AbstractValidator<UserForPasswordUpdateDto>
{
    public UserPasswordUpdateValidator()
    {
        RuleFor(u => u.OldPassword).NotEmpty().WithMessage("Mevcut şifre zorunludur.");
        RuleFor(u => u.NewPassword).StrongPassword();
    }
}
