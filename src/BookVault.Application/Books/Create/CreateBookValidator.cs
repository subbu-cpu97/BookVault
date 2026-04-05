using FluentValidation;

namespace BookVault.Application.Books.Create;

// FluentValidation — declarative validation rules, separate from business logic
// Interview answer: "Why FluentValidation over DataAnnotations?"
// FluentValidation is testable (unit test validators in isolation),
// composable (build rules from smaller rules), and keeps validation
// OUT of your domain models — single responsibility principle.
public class CreateBookValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required.")
            .Matches(@"^(?:\d{9}[\dX]|\d{13})$")
            .WithMessage("ISBN must be a valid 10 or 13 digit ISBN.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.PublishedYear)
            .InclusiveBetween(1000, DateTime.UtcNow.Year + 1)
            .WithMessage("Published year is not valid.");

        RuleFor(x => x.AuthorId)
            .NotEmpty().WithMessage("Author is required.");
    }
}
