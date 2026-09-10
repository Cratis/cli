// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.for_ScreenplayProjectCompilations.when_mapping;

public class with_workspace_source_metadata : for_CritterStackScreenplayGeneration.given.a_marten_application_built_from_source
{
    const string ActualProjectPath = "/physical/checkout/Source/Banking/Banking.csproj";
    const string ActualSourceRoot = "/physical/checkout";
    IReadOnlyList<DotNetProjectCompilation> _result;

    void Establish() => Loaded = Loaded with
    {
        ProjectSources =
        [
            ProjectSource with
            {
                ProjectPath = ActualProjectPath,
                Role = DotNetProjectRole.Application,
                SourceRoot = ActualSourceRoot
            }
        ]
    };

    void Because() => _result = ScreenplayProjectCompilations.From(Loaded, "/legacy/Application.slnx");

    [Fact] void should_align_the_name() => _result[0].Name.ShouldEqual(Loaded.ProjectNames[0]);
    [Fact] void should_align_the_role() => _result[0].Role.ShouldEqual(DotNetProjectRole.Application);
    [Fact] void should_align_the_actual_project_path() => _result[0].ProjectPath.ShouldEqual(ActualProjectPath);
    [Fact] void should_align_the_physical_source_root() => _result[0].SourceRoot.ShouldEqual(ActualSourceRoot);
    [Fact] void should_align_the_source_context() => _result[0].SourceContext.ShouldEqual(ProjectSource.SourceContext);
    [Fact] void should_align_the_compilation() => _result[0].Compilation.ShouldEqual(Loaded.Compilations[0]);
    [Fact] void should_align_the_authored_syntax_trees() => _result[0].AuthoredSyntaxTrees.ShouldEqual(Loaded.AuthoredSyntaxTrees[0]);
}
