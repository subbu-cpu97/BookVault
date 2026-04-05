using FluentValidation;

namespace BookVault.Application.Books.GetAll;

// Interview answer: "Why validate query parameters?"
// A client sending page=-1 or pageSize=0 would cause divide-by-zero
// or negative OFFSET in SQL. Validate at the boundary, before it reaches
// the database. FluentValidation runs via the pipeline behavior — automatic.
public class GetAllBooksQueryValidator : AbstractValidator<GetAllBooksQuery>
{
    private static readonly string[] ValidSortFields =
        ["title", "price", "year", "genre"];

    public GetAllBooksQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("PageSize must be between 1 and 50.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("MinPrice cannot be negative.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("MaxPrice cannot be negative.");

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue ||
                       x.MinPrice <= x.MaxPrice)
            .WithMessage("MinPrice must be less than or equal to MaxPrice.");

        RuleFor(x => x.SortBy)
            .Must(s => ValidSortFields.Contains(s.ToLower()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortFields)}.");
    }
}
