// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_selecting;

public class and_arc_excludes_non_artifact_provenance : given.provider_compilations
{
    LoadedCompilation _result;

    void Because()
    {
        var application = CSharpCompilation.Create(
            "Application",
            [CSharpSyntaxTree.ParseText("namespace Cratis.Arc.Commands.ModelBound { public class CommandAttribute : System.Attribute; }")],
            References());
        var tooling = CSharpCompilation.Create(
            "Tooling",
            [CSharpSyntaxTree.ParseText("public class Tooling;")],
            References());
        var policy = new DotNetSourcePathPolicy
        {
            DisplayRoot = DotNetSourceDisplayRoot.Workspace,
            CasePolicy = DotNetSourcePathCasePolicy.Ordinal
        };
        var loaded = new LoadedCompilation([application, tooling], ["Application", "Tooling"], [])
        {
            AuthoredSyntaxTrees = [application.SyntaxTrees.ToHashSet(), tooling.SyntaxTrees.ToHashSet()],
            ProjectProvenance =
            [
                new ScreenplayProjectProvenance("Application", "net9.0", [], [], []),
                new ScreenplayProjectProvenance("Tooling", "net9.0", [], [], [])
            ],
            ProjectSources =
            [
                new ScreenplayProjectSource(
                    "/physical/Application/Application.csproj",
                    "Application/Application.csproj",
                    DotNetSourcePaths.Create("Application/Application", policy, [])),
                new ScreenplayProjectSource(
                    "/physical/Tooling/Tooling.csproj",
                    "Tooling/Tooling.csproj",
                    DotNetSourcePaths.Create("Tooling/Tooling", policy, []))
            ]
        };

        _result = new ArcSourceProvider().SelectFrom(loaded);
    }

    [Fact] void should_keep_only_the_project_arc_will_interpret() => _result.ProjectNames.ShouldContainOnly(["Application"]);
    [Fact] void should_keep_only_the_matching_authored_trees() => _result.AuthoredSyntaxTrees.Single().Single().ShouldEqual(_result.Compilations.Single().SyntaxTrees.Single());
    [Fact] void should_keep_only_the_matching_project_provenance() => _result.ProjectProvenance.Select(_ => _.Project).ShouldContainOnly(["Application"]);
    [Fact] void should_keep_only_the_matching_project_source() => _result.ProjectSources.Select(_ => _.LogicalProjectPath).ShouldContainOnly(["Application/Application.csproj"]);
}
