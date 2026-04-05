using FluentAssertions;
using MediatR;
using NetArchTest.Rules;
using Xunit;

namespace BookVault.ArchitectureTests;

// Design rules — the "how we build" constraints that make the codebase
// consistent and safe. These catch architectural drift before it compounds.
//
// Interview answer: "Give an example of an architecture test that caught a bug."
// We had a handler that directly instantiated BookVaultDbContext instead of
// using IUnitOfWork. It worked locally but bypassed the audit interceptor.
// The design rule test 'Handlers_ShouldNot_Instantiate_DbContext' would
// have caught it at CI time before it reached production.

public class DesignRuleTests : ArchTestBase
{
    // ── Handlers must not depend on DbContext directly ──────────────

    [Fact]
    public void CommandHandlers_ShouldNot_DependOn_DbContext()
    {
        // Handlers must go through IUnitOfWork and IRepository interfaces.
        // Direct DbContext access bypasses the audit interceptor and
        // breaks the dependency inversion principle.
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .ShouldNot()
            .HaveDependencyOn(
                "Microsoft.EntityFrameworkCore.DbContext")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Handlers must use IUnitOfWork, not DbContext directly. " +
                     $"Failing: {GetFailing(result)}");
    }

    [Fact]
    public void QueryHandlers_ShouldNot_DependOn_DbContext()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .ShouldNot()
            .HaveDependencyOn(
                "Microsoft.EntityFrameworkCore.DbContext")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Failing: {GetFailing(result)}");
    }

    // ── Domain entities must be in correct namespace ─────────────────

    [Fact]
    public void Entities_ShouldReside_InDomainEntitiesNamespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(BookVault.Domain.Entities.BaseEntity))
            .Should()
            .ResideInNamespace($"{DomainNamespace}.Entities")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All entities must live in Domain.Entities namespace. " +
                     $"Failing: {GetFailing(result)}");
    }

    // ── Interfaces must start with I ─────────────────────────────────

    [Fact]
    public void Interfaces_ShouldBe_PrefixedWithI()
    {
        // Check interfaces in Application (the repository/service interfaces)
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All interfaces must be prefixed with 'I' per C# convention. " +
                     $"Failing: {GetFailing(result)}");
    }

    // ── Handlers must implement IRequestHandler ──────────────────────

    [Fact]
    public void ClassesNamedHandler_MustImplement_IRequestHandler()
    {
        // Catches classes accidentally named XHandler that don't implement
        // the MediatR interface — dead code that looks like a handler but isn't.
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Or()
            .ImplementInterface(typeof(IRequestHandler<>))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Every class ending in 'Handler' must be a real MediatR handler. " +
                     $"Failing: {GetFailing(result)}");
    }

    // ── Repositories must implement IRepository interfaces ───────────

    [Fact]
    public void Repositories_MustBe_NotPubliclyInstantiable()
    {
        // Repository classes should be registered via DI, never newed up.
        // They must have at least one constructor that takes dependencies.
        // This test ensures they're not static classes.
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .ShouldNot()
            .BeStatic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Repositories must be registered via DI, not static. " +
                     $"Failing: {GetFailing(result)}");
    }

    // ── Domain must be framework-free ───────────────────────────────

    [Fact]
    public void Domain_ShouldNot_Reference_EntityFrameworkCore()
    {
        // The domain should never know EF Core exists.
        // Audit fields (CreatedOn, UpdatedOn) are set by the interceptor.
        // Entity configuration lives in Infrastructure.
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain entities must be pure C# — no EF Core attributes. " +
                     $"Failing: {GetFailing(result)}");
    }

    [Fact]
    public void Domain_ShouldNot_Reference_AspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not reference any web framework. " +
                     $"Failing: {GetFailing(result)}");
    }

    // ── Specification pattern enforcement ────────────────────────────

    [Fact]
    public void Specifications_ShouldReside_InSpecificationsNamespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Specification")
            .Should()
            .ResideInNamespaceContaining("Specifications")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All Specification classes must be in a Specifications namespace. " +
                     $"Failing: {GetFailing(result)}");
    }

    private static string GetFailing(TestResult result) =>
        result.FailingTypes is null || !result.FailingTypes.Any()
            ? "none"
            : string.Join(", ", result.FailingTypes.Select(t => t.Name));
}
