using FluentValidation;
using Lawyers.Application.Features.Lawyers.Queries;

namespace Lawyers.Application.Features.Lawyers.Validators;

public class GetLawyersQueryValidator : AbstractValidator<GetLawyersQuery>
{
    public GetLawyersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}