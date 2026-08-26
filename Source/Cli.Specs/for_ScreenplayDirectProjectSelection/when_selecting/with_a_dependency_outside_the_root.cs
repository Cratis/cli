// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_ScreenplayDirectProjectSelection.when_selecting;

public class with_a_dependency_outside_the_root : Specification
{
    AdhocWorkspace _workspace;
    Exception _exception;

    void Establish() => _workspace = new AdhocWorkspace();

    async Task Because()
    {
        var application = ProjectId.CreateNewId();
        var dependency = ProjectId.CreateNewId();
        var solution = _workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(application, VersionStamp.Create(), "Application", "Application", LanguageNames.CSharp, filePath: "/workspace/Application.csproj"))
            .AddProject(ProjectInfo.Create(dependency, VersionStamp.Create(), "Dependency", "Dependency", LanguageNames.CSharp, filePath: "/outside/Dependency.csproj"))
            .AddProjectReference(application, new ProjectReference(dependency));
        _workspace.TryApplyChanges(solution);

        var closure = ScreenplayDirectProjectSelection.Select(_workspace.CurrentSolution.GetProject(application)!);
        _exception = await Catch.Exception(() => Task.FromResult(ScreenplayDirectProjectWorkspaceBoundary.Resolve("/workspace/Application.csproj", closure)));
    }

    [Fact] void should_reject_a_filesystem_root_common_boundary() => _exception.ShouldBeOfExactType<InvalidScreenplayProjectSource>();
    [Fact] void should_explain_the_filesystem_root_broadening() => _exception.Message.ShouldContain("filesystem root");

    void Destroy() => _workspace.Dispose();
}
