using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace BookVault.ArchitectureTests;

// Naming conventions make codebases navigable.
// A new developer should be able to find any handler, validator, or
// repository by searching for the suffix — not by reading the whole codebase.
// These tests enforce that convention permanently.
//
// Interview answer: "What naming conventions do you enforce and why?"
// Suffixes communicate intent: *Command tells you it changes state,
// *QueryHandler tells you it reads state, *Repository tells you it touches DB.
// Consistent naming reduces cognitive load — you never have to guess.

public class NamingConventionTests : ArchTestBase
{
    // ── Application layer conventions ───────────────────────────────

    [Fact]
    public void Commands_ShouldBe_NamedWithCommandSuffix()
    {
        // All IRequest<T> implementations that change state should end in Command
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequest<>))
            .And()
            .HaveNameEndingWith("Query")
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // We check commands specifically:
        var commandResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .ResideInNamespace(ApplicationNamespace)
            .GetResult();

        commandResult.IsSuccessful.Should().BeTrue(
            because: "All Command classes must live in the Application layer. " +
                     $"Failing: {GetFailing(commandResult)}");
    }

    [Fact]
    public void CommandHandlers_ShouldBe_NamedWithHandlerSuffix()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .ResideInNamespace(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All CommandHandler classes must live in Application. " +
                     $"Failing: {GetFailing(result)}");
    }

    [Fact]
    public void QueryHandlers_ShouldBe_NamedWithHandlerSuffix()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .ResideInNamespace(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Failing: {GetFailing(result)}");
    }

    [Fact]
    public void Validators_ShouldBe_NamedWithValidatorSuffix()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .ResideInNamespace(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All Validator classes must live in Application. " +
                     $"Failing: {GetFailing(result)}");
    }

    // ── Infrastructure layer conventions ────────────────────────────

    [Fact]
    public void Repositories_ShouldBe_NamedWithRepositorySuffix()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespace(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All Repository implementations must live in Infrastructure. " +
                     $"Failing: {GetFailing(result)}");
    }

    [Fact]
    public void Configurations_ShouldBe_NamedWithConfigurationSuffix()
    {
        // IEntityTypeConfiguration<T> implementations
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Configuration")
            .Should()
            .ResideInNamespace(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Failing: {GetFailing(result)}");
    }

    // ── Domain layer conventions ─────────────────────────────────────

    [Fact]
    public void DomainEvents_ShouldBe_NamedWithEventSuffix()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(BookVault.Domain.Events.IDomainEvent))
            .Should()
            .HaveNameEndingWith("Event")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All domain events must end with 'Event'. " +
                     $"Failing: {GetFailing(result)}");
    }

    [Fact]
    public void Exceptions_ShouldBe_NamedWithExceptionSuffix()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Exception))
            .Should()
            .HaveNameEndingWith("Exception")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Failing: {GetFailing(result)}");
    }

    private static string GetFailing(TestResult result) =>
        result.FailingTypes is null || !result.FailingTypes.Any()
            ? "none"
            : string.Join(", ", result.FailingTypes.Select(t => t.Name));
}
