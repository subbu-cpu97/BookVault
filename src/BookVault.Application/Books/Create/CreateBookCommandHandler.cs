using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using MediatR;

namespace BookVault.Application.Books.Create;

// Handler = the actual logic for this command
// IRequestHandler<TRequest, TResponse> — MediatR wires Command → Handler automatically
// Interview answer: "What is the Handler's single responsibility?"
// Orchestrate: validate inputs exist (author exists?), create domain object,
// persist it, return a response. It does NOT contain business rules —
// those live in the domain entity's Create() factory method.
public class CreateBookCommandHandler(
    IBookRepository bookRepository,
    IAuthorRepository authorRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateBookCommand, CreateBookResponse>
{
    public async Task<CreateBookResponse> Handle(
        CreateBookCommand command,
        CancellationToken ct)
    {
        // 1. Validate the author exists (cross-entity rule)
        var author = await authorRepository.GetByIdAsync(command.AuthorId, ct)
            ?? throw new KeyNotFoundException($"Author {command.AuthorId} not found.");

        // 2. Check ISBN uniqueness
        var isbnTaken = await bookRepository.ExistsByIsbnAsync(command.ISBN, ct);
        if (isbnTaken)
        {
            throw new InvalidOperationException($"A book with ISBN {command.ISBN} already exists.");
        }

        // 3. Create domain entity — business rules enforced inside Book.Create()
        var book = Book.Create(
            command.Title,
            command.ISBN,
            command.Genre,
            command.PublishedYear,
            command.Price,
            command.AuthorId);

        // 4. Persist
        await bookRepository.AddAsync(book, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // 5. Map to response DTO — never leak the domain entity
        return new CreateBookResponse(
            book.Id,
            book.Title,
            book.ISBN,
            book.Genre.ToString(),
            book.PublishedYear,
            book.Price,
            book.AuthorId);
    }
}
