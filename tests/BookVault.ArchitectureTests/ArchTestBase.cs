using System.Reflection;
using NetArchTest.Rules;

namespace BookVault.ArchitectureTests;

// Shared base — all architecture test classes inherit this
// Provides the four assembly objects and helper predicates
// Interview answer: "What does NetArchTest.Rules.Types.InAssembly() do?"
// It loads all types from the given assembly and returns a fluent builder.
// You chain predicates (Should(), HaveNoDependencyOn()) to define the rule.
// .GetResult().IsSuccessful tells you if every type passed.
public abstract class ArchTestBase
{
    // The four assemblies that make up BookVault
    protected static readonly Assembly DomainAssembly =
        typeof(BookVault.Domain.AssemblyReference).Assembly;

    protected static readonly Assembly ApplicationAssembly =
        typeof(BookVault.Application.AssemblyReference).Assembly;

    protected static readonly Assembly InfrastructureAssembly =
        typeof(BookVault.Infrastructure.AssemblyReference).Assembly;

    protected static readonly Assembly ApiAssembly =
        typeof(BookVault.Api.AssemblyReference).Assembly;

    // Namespace constants — avoids magic strings scattered across tests
    protected const string DomainNamespace = "BookVault.Domain";
    protected const string ApplicationNamespace = "BookVault.Application";
    protected const string InfrastructureNamespace = "BookVault.Infrastructure";
    protected const string ApiNamespace = "BookVault.Api";
}
