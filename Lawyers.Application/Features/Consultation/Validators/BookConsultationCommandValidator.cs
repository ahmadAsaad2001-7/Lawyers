using FluentValidation;
using Lawyers.Application.Features.Consultations.Commands;

namespace Lawyers.Application.Features.Consultations.Validators;

public class BookConsultationCommandValidator : AbstractValidator<BookConsultationCommand>
{
    public BookConsultationCommandValidator()
    {
        RuleFor(x => x.LawyerId)
            .GreaterThan(0).WithMessage("A valid Lawyer ID is required.");

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("You cannot book a consultation in the past.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0 minutes.");
    }
}