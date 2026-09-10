// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_ScreenplayWorkspaceProjectSelection.when_selecting;

public class with_duplicate_project_names : Specification
{
    AdhocWorkspace _workspace;
    IReadOnlyList<Project> _result;
    IReadOnlyList<ScreenplayDiagnostic> _diagnostics;

    void Establish() => _workspace = new AdhocWorkspace();

    void Because()
    {
        var root = Path.Combine(Path.GetTempPath(), $"screenplay-duplicate-names-{Guid.NewGuid():N}");
        var first = AddProject(Path.Combine(root, "First", "Shared.csproj"));
        var second = AddProject(Path.Combine(root, "Second", "Shared.csproj"));
        (_result, _diagnostics) = ScreenplayWorkspaceProjectSelection.Select([first, second], null, Path.Combine(root, "Application.slnx"));
    }

    [Fact] void should_keep_both_distinct_projects() => _result.Count.ShouldEqual(2);
    [Fact] void should_keep_both_distinct_project_paths() => _result.Select(_ => _.FilePath).Distinct(StringComparer.Ordinal).Count().ShouldEqual(2);
    [Fact] void should_not_treat_duplicate_names_as_target_framework_variants() => _diagnostics.ShouldBeEmpty();

    void Destroy() => _workspace.Dispose();

    Project AddProject(string filePath) => _workspace.AddProject(ProjectInfo.Create(
        ProjectId.CreateNewId(),
        VersionStamp.Create(),
        "Shared",
        Path.GetDirectoryName(filePath)!,
        LanguageNames.CSharp,
        filePath: filePath));
}
