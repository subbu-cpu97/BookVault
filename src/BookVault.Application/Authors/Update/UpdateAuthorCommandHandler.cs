using BookVault.Application.Common.Interfaces;
using MediatR;

namespace BookVault.Application.Authors.Update;

public class UpdateAuthorCommandHandler(
    IAuthorRepository authorRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateAuthorCommand>
{
    public async Task Handle(UpdateAuthorCommand command, CancellationToken ct)
    {
        var author = await authorRepository.GetByIdAsync(command.Id, ct)
            ?? throw new KeyNotFoundException($"Author {command.Id} not found.");

        author.Update(command.FirstName, command.LastName, command.Bio);

        await authorRepository.AddAsync(author, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
