using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Books.Delete;

public class DeleteBookCommandHandler(
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteBookCommand>
{
    public async Task Handle(DeleteBookCommand command, CancellationToken ct)
    {
        var book = await bookRepository.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"Book {command.Id} not found.");

        await bookRepository.DeleteAsync(book, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
