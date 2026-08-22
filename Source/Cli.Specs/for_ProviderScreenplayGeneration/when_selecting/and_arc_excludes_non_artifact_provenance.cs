// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_selecting;

public class and_arc_excludes_non_artifact_provenance : given.provider_compilations
{
    LoadedCompilation _result;

    void Because()
    {
        var loaded = new LoadedCompilation(
            [
                CSharpCompilation.Create(
                    "Application",
                    [CSharpSyntaxTree.ParseText("namespace Cratis.Arc.Commands.ModelBound { public class CommandAttribute : System.Attribute; }")],
                    References()),
                CSharpCompilation.Create(
                    "Tooling",
                    [CSharpSyntaxTree.ParseText("public class Tooling;")],
                    References())
            ],
            ["Application", "Tooling"],
            [])
        {
            ProjectProvenance =
            [
                new ScreenplayProjectProvenance("Application", "net9.0", [], [], []),
                new ScreenplayProjectProvenance("Tooling", "net9.0", [], [], [])
            ]
        };

        _result = new ArcSourceProvider().SelectFrom(loaded);
    }

    [Fact] void should_keep_only_the_project_arc_will_interpret() => _result.ProjectNames.ShouldContainOnly(["Application"]);
    [Fact] void should_keep_only_the_matching_project_provenance() => _result.ProjectProvenance.Select(_ => _.Project).ShouldContainOnly(["Application"]);
}
