// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Cli.for_ScreenplayProjectSources.when_creating;

public class with_invalid_paths : Specification
{
    List<AdhocWorkspace> _workspaces;
    Exception _rooted;
    Exception _traversing;
    Exception _outsideRoot;
    Exception _duplicate;
    Exception _unmapped;

    void Establish() => _workspaces = [];

    async Task Because()
    {
        _rooted = await FailureFrom([Path.DirectorySeparatorChar.ToString()], "Rooted.cs", "Rooted.cs");
        _traversing = await FailureFrom([".."], "Traversing.cs", "Traversing.cs");
        _outsideRoot = await FailureFrom([], "Outside.cs", Path.Combine("..", "Outside.cs"));
        _duplicate = await DuplicateFailure();
        _unmapped = await UnmappedFailure();
    }

    [Fact] void should_reject_a_rooted_logical_path() => _rooted.ShouldBeOfExactType<InvalidScreenplayProjectSource>();
    [Fact] void should_reject_a_traversing_logical_path() => _traversing.ShouldBeOfExactType<InvalidScreenplayProjectSource>();
    [Fact] void should_reject_a_document_outside_the_workspace_root() => _outsideRoot.ShouldBeOfExactType<InvalidScreenplayProjectSource>();
    [Fact] void should_reject_a_duplicate_file_identity() => _duplicate.ShouldBeOfExactType<InvalidScreenplayProjectSource>();
    [Fact] void should_reject_an_unmapped_authored_tree() => _unmapped.ShouldBeOfExactType<InvalidScreenplayProjectSource>();

    void Destroy() => _workspaces.ForEach(workspace => workspace.Dispose());

    async Task<Exception> FailureFrom(IReadOnlyList<string> folders, string name, string relativePhysicalPath)
    {
        var (project, root) = ProjectWithDocument(folders, name, relativePhysicalPath);
        var compilation = await project.GetCompilationAsync() ?? throw new SourceFixtureCompilationFailed();
        return await FailureFrom(project, compilation, root);
    }

    async Task<Exception> DuplicateFailure()
    {
        var (project, root) = ProjectWithDocument(["Links"], "Shared.cs", Path.Combine("Shared", "First.cs"));
        var solution = project.Solution.AddDocument(
            DocumentId.CreateNewId(project.Id),
            "Shared.cs",
            SourceText.From("public record Second;"),
            ["Links"],
            Path.Combine(root, "Shared", "Second.cs"));
        project = solution.GetProject(project.Id)!;
        var compilation = await project.GetCompilationAsync() ?? throw new SourceFixtureCompilationFailed();
        return await FailureFrom(project, compilation, root);
    }

    async Task<Exception> UnmappedFailure()
    {
        var (project, root) = ProjectWithDocument([], "First.cs", "First.cs");
        var compilation = await project.GetCompilationAsync() ?? throw new SourceFixtureCompilationFailed();
        var solution = project.Solution.AddDocument(
            DocumentId.CreateNewId(project.Id),
            "Second.cs",
            SourceText.From("public record Second;"),
            filePath: Path.Combine(root, "Second.cs"));
        project = solution.GetProject(project.Id)!;
        return await FailureFrom(project, compilation, root);
    }

    async Task<Exception> FailureFrom(Project project, Compilation compilation, string root)
    {
        try
        {
            await ScreenplayProjectSources.Create(project, compilation, root, usesWorkspaceDisplayRoot: true, CancellationToken.None);
            return new SourcePathFailureWasNotReported();
        }
        catch (InvalidScreenplayProjectSource exception)
        {
            return exception;
        }
    }

    (Project Project, string Root) ProjectWithDocument(
        IReadOnlyList<string> folders,
        string name,
        string relativePhysicalPath)
    {
        var workspace = new AdhocWorkspace();
        _workspaces.Add(workspace);
        var root = Path.Combine(Path.GetTempPath(), $"screenplay-invalid-{Guid.NewGuid():N}");
        var project = workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "Application",
            "Application",
            LanguageNames.CSharp,
            filePath: Path.Combine(root, "Application.csproj")));
        var solution = project.Solution.AddDocument(
            DocumentId.CreateNewId(project.Id),
            name,
            SourceText.From("public record First;"),
            folders,
            Path.GetFullPath(relativePhysicalPath, root));
        return (solution.GetProject(project.Id)!, root);
    }
}

sealed class SourcePathFailureWasNotReported : Exception;
