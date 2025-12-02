using Entities.DTOs.User;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation;

public class UserRegisterValidator : AbstractValidator<UserForRegisterDto>
{
    public UserRegisterValidator()
    {
        RuleFor(u => u.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.");

        RuleFor(u => u.Name).NotEmpty().WithMessage("İsim boş bırakılamaz.");
        RuleFor(u => u.Surname).NotEmpty().WithMessage("Soyisim boş bırakılamaz.");

        RuleFor(u => u.Email)
            .NotEmpty().WithMessage("Email adresi zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

        RuleFor(u => u.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");
    }
}