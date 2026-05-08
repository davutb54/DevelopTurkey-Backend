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

        RuleFor(p => p.Address)
            .MaximumLength(500).WithMessage("Adres 500 karakterden uzun olamaz.")
            .When(p => !string.IsNullOrWhiteSpace(p.Address));

        RuleFor(p => p)
            .Must(p => (p.Latitude.HasValue && p.Longitude.HasValue) || (!p.Latitude.HasValue && !p.Longitude.HasValue))
            .WithMessage("Konum için enlem ve boylam birlikte gönderilmelidir.");

        RuleFor(p => p.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Enlem -90 ile 90 arasında olmalıdır.")
            .When(p => p.Latitude.HasValue);

        RuleFor(p => p.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Boylam -180 ile 180 arasında olmalıdır.")
            .When(p => p.Longitude.HasValue);

        //RuleFor(p => p.TopicId).GreaterThan(0).WithMessage("Lütfen geçerli bir konu başlığı seçin.");
    }
}