using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Books.Update;

public class UpdateBookCommandHandler(
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateBookCommand>
{
    public async Task Handle(UpdateBookCommand command, CancellationToken ct)
    {
        var book = await bookRepository.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"Book {command.Id} not found.");

        // Business rules enforced inside the domain entity — not here
        book.Update(command.Title, command.Genre, command.Price);

        await bookRepository.UpdateAsync(book, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
