// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Cli.for_ScreenplayProjectSources.when_creating;

public class from_a_solution_with_a_linked_document : Specification
{
    List<AdhocWorkspace> _workspaces;
    ScreenplayProjectSource _first;
    ScreenplayProjectSource _relocated;

    void Establish() => _workspaces = [];

    async Task Because()
    {
        _first = await SourceFrom(Path.Combine(Path.GetTempPath(), $"screenplay-solution-a-{Guid.NewGuid():N}"));
        _relocated = await SourceFrom(Path.Combine(Path.GetTempPath(), $"screenplay-solution-b-{Guid.NewGuid():N}"));
    }

    [Fact] void should_use_the_workspace_display_root() => _first.SourceContext.Policy.DisplayRoot.ShouldEqual(DotNetSourceDisplayRoot.Workspace);
    [Fact] void should_keep_the_workspace_relative_project_path() => _first.LogicalProjectPath.ShouldEqual("Source/Application/Application.csproj");
    [Fact] void should_derive_identity_from_the_logical_project_path() => _first.SourceContext.ProjectIdentity.ShouldEqual("Source/Application/Application");
    [Fact] void should_display_the_linked_document_from_the_workspace() => _first.SourceContext.Files.Values.Single().DisplayPath.ShouldEqual("Shared/PlaceOrder.cs");
    [Fact] void should_identify_the_linked_document_from_folders_and_name() => _first.SourceContext.Files.Values.Single().Identity.Path.ShouldEqual("Links/SharedOrder.cs");
    [Fact] void should_preserve_logical_metadata_after_relocation() => LogicalDescription(_relocated).ShouldEqual(LogicalDescription(_first));
    [Fact] void should_change_only_the_internal_actual_project_path_after_relocation() => _relocated.ProjectPath.ShouldNotEqual(_first.ProjectPath);

    void Destroy() => _workspaces.ForEach(workspace => workspace.Dispose());

    async Task<ScreenplayProjectSource> SourceFrom(string root)
    {
        var workspace = new AdhocWorkspace();
        _workspaces.Add(workspace);
        var projectPath = Path.Combine(root, "Source", "Application", "Application.csproj");
        var linkedPath = Path.Combine(root, "Shared", "PlaceOrder.cs");
        var project = workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "Application",
            "Application",
            LanguageNames.CSharp,
            filePath: projectPath));
        var solution = project.Solution.AddDocument(
            DocumentId.CreateNewId(project.Id),
            "SharedOrder.cs",
            SourceText.From("public record PlaceOrder;"),
            ["Links"],
            linkedPath);
        project = solution.GetProject(project.Id)!;
        var compilation = await project.GetCompilationAsync() ?? throw new SourceFixtureCompilationFailed();

        return (await ScreenplayProjectSources.Create(project, compilation, root, usesWorkspaceDisplayRoot: true, CancellationToken.None)).Source;
    }

    static string LogicalDescription(ScreenplayProjectSource source)
    {
        var file = source.SourceContext.Files.Values.Single();
        return $"{source.LogicalProjectPath}|{source.SourceContext.ProjectIdentity}|{source.SourceContext.Policy.Version}|{source.SourceContext.Policy.DisplayRoot}|{source.SourceContext.Policy.CasePolicy}|{file.Identity}|{file.DisplayPath}";
    }
}
