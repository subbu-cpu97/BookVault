using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using MediatR;

namespace BookVault.Application.Authors.Create;

public class CreateAuthorCommandHandler(
    IAuthorRepository authorRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateAuthorCommand, CreateAuthorResponse>
{
    public async Task<CreateAuthorResponse> Handle(
        CreateAuthorCommand command,
        CancellationToken ct)
    {
        // Factory method on the domain entity handles creation + raises domain event
        var author = Author.Create(command.FirstName, command.LastName, command.Bio);

        await authorRepository.AddAsync(author, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateAuthorResponse(
            author.Id,
            author.FirstName,
            author.LastName,
            author.FullName,
            author.Bio
        );
    }
}
