// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_ScreenplayDirectProjectSelection.when_ordering;

public class with_relocated_multi_target_projects : Specification
{
    AdhocWorkspace _firstWorkspace;
    AdhocWorkspace _relocatedWorkspace;
    IReadOnlyList<string> _first;
    IReadOnlyList<string> _relocated;

    void Establish()
    {
        _firstWorkspace = new AdhocWorkspace();
        _relocatedWorkspace = new AdhocWorkspace();
    }

    void Because()
    {
        _first = OrderedNames(_firstWorkspace, "/physical/first");
        _relocated = OrderedNames(_relocatedWorkspace, "/physical/relocated");
    }

    [Fact] void should_order_by_logical_path_name_and_target_framework() => _first.SequenceEqual(["Application(net10.0)", "Application(net9.0)", "Shared"]).ShouldBeTrue();
    [Fact] void should_preserve_order_after_relocation() => _relocated.ShouldEqual(_first);

    void Destroy()
    {
        _firstWorkspace.Dispose();
        _relocatedWorkspace.Dispose();
    }

    static IReadOnlyList<string> OrderedNames(AdhocWorkspace workspace, string root)
    {
        Project[] projects =
        [
            AddProject(workspace, "Shared", Path.Combine(root, "Shared", "Shared.csproj")),
            AddProject(workspace, "Application(net9.0)", Path.Combine(root, "Application.csproj")),
            AddProject(workspace, "Application(net10.0)", Path.Combine(root, "Application.csproj"))
        ];
        return [.. ScreenplayDirectProjectSelection.Order(projects, root).Select(_ => _.Name)];
    }

    static Project AddProject(AdhocWorkspace workspace, string name, string filePath) =>
        workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            name,
            name,
            LanguageNames.CSharp,
            filePath: filePath));
}
