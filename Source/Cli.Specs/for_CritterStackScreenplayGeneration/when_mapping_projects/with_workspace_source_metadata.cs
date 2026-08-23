// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_mapping_projects;

public class with_workspace_source_metadata : given.a_marten_application_built_from_source
{
    const string ActualProjectPath = "/physical/checkout/Source/Banking/Banking.csproj";
    DotNetProjectSourceContext _sourceContext;
    IReadOnlyList<DotNetProjectCompilation> _result;

    void Establish()
    {
        var syntaxTree = Loaded.Compilations[0].SyntaxTrees.Single();
        _sourceContext = DotNetSourcePaths.Create(
            "Source/Banking/Banking",
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
                    WorkspaceRelativePath = "Source/Banking/Account.cs"
                }
            ]);
        Loaded = Loaded with
        {
            ProjectSources =
            [
                new ScreenplayProjectSource(
                    ActualProjectPath,
                    "Source/Banking/Banking.csproj",
                    _sourceContext)
            ]
        };
    }

    void Because() => _result = CritterStackScreenplayGeneration.ProjectsFrom(Loaded, "/physical/checkout/Application.slnx");

    [Fact] void should_pass_the_actual_project_path() => _result[0].ProjectPath.ShouldEqual(ActualProjectPath);
    [Fact] void should_pass_the_source_context() => _result[0].SourceContext.ShouldEqual(_sourceContext);
    [Fact] void should_keep_the_legacy_source_root_fallback() => _result[0].SourceRoot.ShouldEqual("/physical/checkout");
}
