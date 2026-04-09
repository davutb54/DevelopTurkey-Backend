using FluentValidation;

namespace Business.ValidationRules.FluentValidation;

public static class PasswordRuleExtensions
{
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter içermelidir (!@#$%^ vb.).");
    }
}
