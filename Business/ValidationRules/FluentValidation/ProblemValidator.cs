using Entities.DTOs;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation;

public class ProblemValidator : AbstractValidator<ProblemAddDto>
{
    public ProblemValidator()
    {
        RuleFor(p => p.Title)
            .NotEmpty().WithMessage("Başlık boş olamaz.")
            .MinimumLength(5).WithMessage("Başlık en az 5 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Başlık 100 karakterden uzun olamaz.");

        RuleFor(p => p.Description)
            .NotEmpty().WithMessage("Açıklama boş olamaz.")
            .MinimumLength(20).WithMessage("Lütfen sorunu en az 20 karakterle açıklayın.");

        RuleFor(p => p.CityCode).GreaterThan(0).WithMessage("Lütfen geçerli bir şehir seçin.");
        RuleFor(p => p.TopicId).GreaterThan(0).WithMessage("Lütfen geçerli bir konu başlığı seçin.");
    }
}