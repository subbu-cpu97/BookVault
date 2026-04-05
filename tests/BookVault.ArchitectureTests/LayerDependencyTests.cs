using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace BookVault.ArchitectureTests;

// These tests enforce Clean Architecture's dependency rule:
// Dependencies point INWARD only.
// If any of these fail, a developer has violated the architecture.
// The CI pipeline catches it before it merges — no code review needed.
//
// Interview answer: "Why write tests for architecture rules?"
// Code reviews miss things, especially in large PRs. Architecture violations
// are subtle — a single 'using' statement in the wrong file.
// Automated tests catch every violation, every time, on every branch.
// NetArchTest runs in milliseconds — it reflects over assemblies, no DB needed.

public class LayerDependencyTests : ArchTestBase
{
    // ── Domain rules ────────────────────────────────────────────────

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        // ARRANGE + ACT
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .GetResult();

        // ASSERT
        result.IsSuccessful.Should().BeTrue(
            because: "Domain is the innermost layer — " +
                     "it must not know Application exists. " +
                     $"Failing types: {GetFailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must never reference EF Core, Postgres, or " +
                     "any infrastructure concern. " +
                     $"Failing types: {GetFailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Domain must not reference API layer. " +
                     $"Failing types: {GetFailingTypes(result)}");
    }

    // ── Application rules ───────────────────────────────────────────

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        // This is the most important rule in Clean Architecture.
        // Application defines WHAT it needs via interfaces (IBookRepository).
        // Infrastructure defines HOW (BookRepository : IBookRepository).
        // If Application referenced Infrastructure directly, the entire
        // point of dependency inversion would be lost — you couldn't
        // swap implementations or unit test without a real database.
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application must use interfaces only — never concrete " +
                     "Infrastructure types like BookVaultDbContext or EF Core. " +
                     $"Failing types: {GetFailingTypes(result)}");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Application must not know about HTTP or API concerns. " +
                     $"Failing types: {GetFailingTypes(result)}");
    }

    // ── Infrastructure rules ────────────────────────────────────────

    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure must not reference HTTP endpoints or " +
                     "Scalar/OpenAPI concerns. " +
                     $"Failing types: {GetFailingTypes(result)}");
    }

    // Helper — formats failing type names for readable failure messages
    private static string GetFailingTypes(TestResult result) =>
        result.FailingTypes is null || !result.FailingTypes.Any()
            ? "none"
            : string.Join(", ", result.FailingTypes.Select(t => t.Name));
}
