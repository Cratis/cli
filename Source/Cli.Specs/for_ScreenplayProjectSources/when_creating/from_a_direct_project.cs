// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Cli.for_ScreenplayProjectSources.when_creating;

public class from_a_direct_project : Specification
{
    AdhocWorkspace _workspace;
    string _projectPath;
    ScreenplayProjectSource _source;
    IReadOnlySet<SyntaxTree> _authoredSyntaxTrees;

    void Establish() => _workspace = new AdhocWorkspace();

    async Task Because()
    {
        var root = Path.Combine(Path.GetTempPath(), $"screenplay-direct-{Guid.NewGuid():N}");
        _projectPath = Path.Combine(root, "Application.csproj");
        var documentPath = Path.Combine(root, "Features", "PlaceOrder.cs");
        var project = AddProject(_projectPath, documentPath, ["Features"]);
        var compilation = await project.GetCompilationAsync() ?? throw new SourceFixtureCompilationFailed();

        (_authoredSyntaxTrees, _source) = await ScreenplayProjectSources.Create(
            project,
            compilation,
            root,
            isSolution: false,
            CancellationToken.None);
    }

    [Fact] void should_keep_the_actual_project_path_for_internal_propagation() => _source.ProjectPath.ShouldEqual(_projectPath);
    [Fact] void should_use_a_relocation_safe_logical_project_path() => _source.LogicalProjectPath.ShouldEqual("Application.csproj");
    [Fact] void should_derive_identity_without_the_project_extension() => _source.SourceContext.ProjectIdentity.ShouldEqual("Application");
    [Fact] void should_use_the_project_display_root() => _source.SourceContext.Policy.DisplayRoot.ShouldEqual(DotNetSourceDisplayRoot.Project);
    [Fact] void should_use_ordinal_case_policy() => _source.SourceContext.Policy.CasePolicy.ShouldEqual(DotNetSourcePathCasePolicy.Ordinal);
    [Fact] void should_map_the_authored_tree_once() => _source.SourceContext.Files.Keys.ShouldContainOnly(_authoredSyntaxTrees);
    [Fact] void should_display_the_document_from_its_logical_project_folder() => _source.SourceContext.Files.Values.Single().DisplayPath.ShouldEqual("Features/PlaceOrder.cs");
    [Fact] void should_use_the_logical_project_path_for_file_identity() => _source.SourceContext.Files.Values.Single().Identity.Path.ShouldEqual("Features/PlaceOrder.cs");

    void Destroy() => _workspace.Dispose();

    Project AddProject(string projectPath, string documentPath, IReadOnlyList<string> folders)
    {
        var project = _workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "Application",
            "Application",
            LanguageNames.CSharp,
            filePath: projectPath));
        var solution = project.Solution.AddDocument(
            DocumentId.CreateNewId(project.Id),
            "PlaceOrder.cs",
            SourceText.From("public record PlaceOrder;"),
            folders,
            documentPath);
        return solution.GetProject(project.Id)!;
    }
}

sealed class SourceFixtureCompilationFailed : Exception;
