using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetArchTest.Rules;
using Trackify.Application.Trains;
using Trackify.Cli.Commands;
using Trackify.Domain.Trains;
using Trackify.Infrastructure.Persistence;

namespace Trackify.Tests.Architecture;

/// <summary>
/// Enforces the Clean Architecture dependency rule — dependencies point inward only:
/// <c>Domain ← Application ← Infrastructure ← front-ends (CLI / Uno)</c>. Also guards the DTO
/// boundary from the DTO refactor: a front-end must never touch the Domain entity namespace, it
/// works only with <see cref="TrainDto"/>. These run in CI so a wrong <c>using</c> fails the build.
/// </summary>
public class LayerTrainDependencyTests
{
    private const string Domain = "Trackify.Domain";
    private const string Application = "Trackify.Application";
    private const string Infrastructure = "Trackify.Infrastructure";
    private const string Cli = "Trackify.Cli";

    // The Domain ENTITY namespace specifically (not the enums, which are shared value types).
    private const string DomainEntities = "Trackify.Domain.Trains";

    // Frameworks that belong to the outer layers only.
    private const string EntityFramework = "Microsoft.EntityFrameworkCore";
    private const string SharpBrick = "SharpBrick";

    private static readonly Assembly DomainAssembly = typeof(Train).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(TrainService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(SqliteTrainRepository).Assembly;
    private static readonly Assembly CliAssembly = typeof(DashboardCommand).Assembly;

    [Fact]
    public void Domain_depends_on_no_other_layer()
        => AssertNoDependency(DomainAssembly, Application, Infrastructure, Cli);

    [Fact]
    public void Domain_is_pure_and_free_of_infrastructure_frameworks()
        => AssertNoDependency(DomainAssembly, EntityFramework, SharpBrick);

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_frontends()
        => AssertNoDependency(ApplicationAssembly, Infrastructure, Cli);

    [Fact]
    public void Application_does_not_leak_persistence_frameworks()
        => AssertNoDependency(ApplicationAssembly, EntityFramework);

    [Fact]
    public void Infrastructure_does_not_depend_on_frontends()
        => AssertNoDependency(InfrastructureAssembly, Cli);

    [Fact]
    public void Cli_never_touches_the_domain_entity_namespace()
        => AssertNoDependency(CliAssembly, DomainEntities);

    private static void AssertNoDependency(Assembly assembly, params string[] forbiddenNamespaces)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        var offenders = result.FailingTypeNames ?? Enumerable.Empty<string>();
        Assert.True(result.IsSuccessful,
            $"{assembly.GetName().Name} must not depend on [{string.Join(", ", forbiddenNamespaces)}]. " +
            $"Offending types: {string.Join(", ", offenders)}");
    }
}
