// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class with_an_artifact_owned_by_a_referenced_project : Specification
{
    GeneratedScreenplay _result;

    void Because()
    {
        var domain = CompilationFrom(
            "Domain",
            "namespace Banking.Accounts.Opening { public record AccountOpened(System.Guid AccountId); public class Account { public System.Guid Id { get; set; } public void Apply(AccountOpened opened) { } } }",
            "/workspace/Domain/Features/Banking/Accounts/Opening/Account.cs");
        var host = CompilationFrom(
            "Host",
            "namespace Marten { public interface IDocumentStore; public class StoreOptions { public Marten.Events.Projections.ProjectionOptions Projections { get; } = new(); } } namespace Marten.Events.Projections { public enum SnapshotLifecycle { Inline } public class ProjectionOptions { public void Snapshot<T>(SnapshotLifecycle lifecycle) { } } } public static class Configuration { public static void Configure(Marten.StoreOptions options) => options.Projections.Snapshot<Banking.Accounts.Opening.Account>(Marten.Events.Projections.SnapshotLifecycle.Inline); }",
            "/workspace/Host/Configuration.cs",
            domain.ToMetadataReference());
        var loaded = new LoadedCompilation([domain, host], ["Domain", "Host"], [])
        {
            AuthoredSyntaxTrees = [domain.SyntaxTrees.ToHashSet(), host.SyntaxTrees.ToHashSet()],
            ProjectSources =
            [
                SourceFor(domain, "Domain/Domain.csproj", "Domain", "Features/Banking/Accounts/Opening/Account.cs"),
                SourceFor(host, "Host/Host.csproj", "Host", "Configuration.cs")
            ]
        };

        _result = CritterStackScreenplayGeneration.GenerateFrom(
            loaded,
            "/workspace/Application.slnx",
            ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten, Domain = "Banking", FeatureRoot = "Features" });
    }

    [Fact] void should_resolve_the_referenced_project_as_the_source_owner() => _result.Source.ShouldContain("readmodel Account");
    [Fact] void should_not_report_a_missing_source_owner() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain("DOTNETSP0012");
    [Fact] void should_not_report_a_conflicting_source_owner() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain("DOTNETSP0013");
    [Fact] void should_apply_feature_root_to_the_owning_project() => _result.Diagnostics.Any(_ => _.Code.StartsWith("DOTNETSP", StringComparison.Ordinal)).ShouldBeFalse();

    static CSharpCompilation CompilationFrom(string name, string source, string path, params MetadataReference[] additionalReferences) =>
        CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source, path: path)],
            [.. PlatformReferences(), .. additionalReferences],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    static ScreenplayProjectSource SourceFor(
        Compilation compilation,
        string logicalProjectPath,
        string projectIdentity,
        string projectRelativePath) =>
        new(
            $"/workspace/{logicalProjectPath}",
            logicalProjectPath,
            DotNetSourcePaths.Create(
                projectIdentity,
                new DotNetSourcePathPolicy
                {
                    DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                    CasePolicy = DotNetSourcePathCasePolicy.Ordinal
                },
                [
                    new DotNetSourceDocument
                    {
                        SyntaxTree = compilation.SyntaxTrees.Single(),
                        ProjectRelativePath = projectRelativePath,
                        WorkspaceRelativePath = $"{Path.GetDirectoryName(logicalProjectPath)}/{projectRelativePath}".Replace('\\', '/')
                    }
                ]))
        {
            Role = DotNetProjectRole.Application,
            SourceRoot = "/workspace"
        };

    static IEnumerable<MetadataReference> PlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
}
