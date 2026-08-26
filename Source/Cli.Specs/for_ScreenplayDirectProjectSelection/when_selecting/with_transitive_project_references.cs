// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_ScreenplayDirectProjectSelection.when_selecting;

public class with_transitive_project_references : Specification
{
    AdhocWorkspace _workspace;
    IReadOnlyList<Project> _result;

    void Establish() => _workspace = new AdhocWorkspace();

    void Because()
    {
        const string root = "/workspace";
        var solution = _workspace.CurrentSolution;
        (solution, var application) = AddProject(solution, "Application", "/workspace/Application.csproj");
        (solution, var shared) = AddProject(solution, "Shared", "/workspace/Shared/Shared.csproj");
        (solution, var core) = AddProject(solution, "Core", "/workspace/Core/Core.csproj");
        (solution, var specifications) = AddProject(solution, "Application.Specs", "/workspace/Application.Specs/Application.Specs.csproj");
        (solution, _) = AddProject(solution, "Unrelated", "/workspace/Unrelated/Unrelated.csproj");
        (solution, var reverse) = AddProject(solution, "Reverse", "/workspace/Reverse/Reverse.csproj");
        solution = solution
            .AddProjectReference(application, new ProjectReference(shared))
            .AddProjectReference(shared, new ProjectReference(core))
            .AddProjectReference(application, new ProjectReference(specifications))
            .AddProjectReference(reverse, new ProjectReference(application));
        _workspace.TryApplyChanges(solution);

        _result = ScreenplayDirectProjectSelection.Order(
            ScreenplayDirectProjectSelection.Select(_workspace.CurrentSolution.GetProject(application)!),
            root);
    }

    [Fact] void should_start_from_a_workspace_that_contains_every_exclusion_candidate() => _workspace.CurrentSolution.Projects.Select(_ => _.Name).ShouldContainOnly(["Application", "Shared", "Core", "Application.Specs", "Unrelated", "Reverse"]);
    [Fact] void should_include_only_the_direct_project_and_transitive_dependencies() => _result.Select(_ => _.Name).ShouldContainOnly(["Application", "Core", "Shared"]);
    [Fact] void should_exclude_specifications() => _result.Select(_ => _.Name).ShouldNotContain("Application.Specs");
    [Fact] void should_exclude_unrelated_projects() => _result.Select(_ => _.Name).ShouldNotContain("Unrelated");
    [Fact] void should_exclude_reverse_references() => _result.Select(_ => _.Name).ShouldNotContain("Reverse");
    [Fact] void should_order_by_relocation_safe_logical_path() => _result.Select(_ => _.Name).ShouldEqual(["Application", "Core", "Shared"]);

    void Destroy() => _workspace.Dispose();

    static (Solution Solution, ProjectId Id) AddProject(Solution solution, string name, string filePath)
    {
        var id = ProjectId.CreateNewId();
        return (solution.AddProject(ProjectInfo.Create(id, VersionStamp.Create(), name, name, LanguageNames.CSharp, filePath: filePath)), id);
    }
}
