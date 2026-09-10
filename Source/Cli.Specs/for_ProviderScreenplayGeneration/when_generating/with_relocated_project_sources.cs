// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class with_relocated_project_sources : given.an_application_scope
{
    GeneratedScreenplay _first;
    GeneratedScreenplay _relocated;

    async Task Because()
    {
        _first = await Generate(ContextAt("/physical/first"), ScreenplayProviders.Marten);
        _relocated = await Generate(ContextAt("/physical/relocated"), ScreenplayProviders.Marten);
    }

    [Fact] void should_generate_identical_bytes() => _relocated.Source.ShouldEqual(_first.Source);
    [Fact] void should_generate_identical_diagnostics() => _relocated.Diagnostics.ShouldEqual(_first.Diagnostics);
    [Fact] void should_generate_identical_provenance() => JsonSerializer.Serialize(_relocated.Provenance).ShouldEqual(JsonSerializer.Serialize(_first.Provenance));
    [Fact] void should_not_leak_either_physical_root() => JsonSerializer.Serialize(_relocated.Provenance).ShouldNotContain("/physical/");

    static LoadedCompilation ContextAt(string physicalRoot)
    {
        var loaded = LoadedFrom(Project("Persistence", MartenPackages, false, MartenSource));
        var syntaxTree = loaded.Compilations.Single().SyntaxTrees.Single();
        var sourceContext = DotNetSourcePaths.Create(
            "Source/Persistence/Persistence",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = syntaxTree,
                    ProjectRelativePath = "Account.cs",
                    WorkspaceRelativePath = "Source/Persistence/Account.cs"
                }
            ]);
        return loaded with
        {
            ProjectSources =
            [
                new ScreenplayProjectSource(
                    Path.Combine(physicalRoot, "Source", "Persistence", "Persistence.csproj"),
                    "Source/Persistence/Persistence.csproj",
                    sourceContext)
                {
                    Role = DotNetProjectRole.Application,
                    SourceRoot = physicalRoot
                }
            ]
        };
    }
}
