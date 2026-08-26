// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class and_folder_and_namespace_placement_conflict : Specification
{
    GeneratedScreenplay _result;

    void Because()
    {
        const string source = "namespace Marten { public interface IDocumentStore; public class StoreOptions { public Marten.Events.Projections.ProjectionOptions Projections { get; } = new(); } } namespace Marten.Events.Projections { public enum SnapshotLifecycle { Inline } public class ProjectionOptions { public void Snapshot<T>(SnapshotLifecycle lifecycle) { } } } namespace Banking.Accounts.Opening { public record AccountOpened(System.Guid AccountId); public class Account { public System.Guid Id { get; set; } public void Apply(AccountOpened opened) { } } } public static class Configuration { public static void Configure(Marten.StoreOptions options) => options.Projections.Snapshot<Banking.Accounts.Opening.Account>(Marten.Events.Projections.SnapshotLifecycle.Inline); }";
        var compilation = CSharpCompilation.Create(
            "Banking",
            [CSharpSyntaxTree.ParseText(source, path: "/workspace/Banking/Commerce/Accounts/Opening/Account.cs")],
            PlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var context = DotNetSourcePaths.Create(
            "Banking/Banking",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = compilation.SyntaxTrees.Single(),
                    ProjectRelativePath = "Commerce/Accounts/Opening/Account.cs",
                    WorkspaceRelativePath = "Banking/Commerce/Accounts/Opening/Account.cs"
                }
            ]);
        var loaded = new LoadedCompilation([compilation], ["Banking"], [])
        {
            AuthoredSyntaxTrees = [compilation.SyntaxTrees.ToHashSet()],
            ProjectSources =
            [
                new ScreenplayProjectSource("/workspace/Banking/Banking.csproj", "Banking/Banking.csproj", context)
                {
                    Role = DotNetProjectRole.Application,
                    SourceRoot = "/workspace"
                }
            ]
        };

        _result = CritterStackScreenplayGeneration.GenerateFrom(
            loaded,
            "/workspace/Banking/Banking.csproj",
            ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten });
    }

    [Fact] void should_propagate_the_conflicting_structure_error() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("DOTNETSP0005");
    [Fact] void should_not_place_the_read_model_with_flat_compatibility() => _result.Source.ShouldNotContain("readmodel Account");

    static IEnumerable<MetadataReference> PlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
}
