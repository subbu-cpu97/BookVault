using BookVault.Application.Authors.Create;
using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace BookVault.UnitTests.Application.Authors;

public class CreateAuthorCommandHandlerTests
{
    private readonly IAuthorRepository _authorRepository = Substitute.For<IAuthorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateAuthorCommandHandler _handler;

    public CreateAuthorCommandHandlerTests()
    {
        _handler = new CreateAuthorCommandHandler(_authorRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCreateAuthorResponse()
    {
        // Arrange
        var command = new CreateAuthorCommand("Robert", "Martin", "Clean Code author");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Robert");
        result.LastName.Should().Be("Martin");
        result.FullName.Should().Be("Robert Martin");
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsAuthorAndSaves()
    {
        // Arrange
        var command = new CreateAuthorCommand("Robert", "Martin", "bio");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — verify persistence calls
        await _authorRepository.Received(1).AddAsync(
            Arg.Any<Author>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyFirstName_ThrowsArgumentException()
    {
        // The domain entity enforces this — we verify it propagates
        var command = new CreateAuthorCommand("", "Martin", "bio");
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
