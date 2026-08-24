// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Cli.for_ScreenplayProjectSources.when_creating;

/// <summary>
/// Characterizes canonical workspace containment through a physical directory link.
/// </summary>
public class with_an_in_workspace_link_to_an_outside_document : Specification
{
    AdhocWorkspace _workspace;
    string _fixtureRoot;
    Exception _directLinkError;
    Exception _parentTraversalError;
    bool _isApplicable;

    void Establish() => _workspace = new AdhocWorkspace();

    async Task Because()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        _isApplicable = true;
        _fixtureRoot = Path.Combine(Path.GetTempPath(), $"screenplay-linked-outside-{Guid.NewGuid():N}");
        var workspaceRoot = Path.Combine(_fixtureRoot, "Workspace");
        var outsideRoot = Path.Combine(_fixtureRoot, "Outside");
        var nestedOutsideRoot = Path.Combine(outsideRoot, "Nested");
        Directory.CreateDirectory(workspaceRoot);
        Directory.CreateDirectory(nestedOutsideRoot);
        await File.WriteAllTextAsync(Path.Combine(nestedOutsideRoot, "Outside.cs"), "public record Outside;");
        await File.WriteAllTextAsync(Path.Combine(outsideRoot, "Escaped.cs"), "public record Escaped;");
        var linkedRoot = Directory.CreateSymbolicLink(Path.Combine(workspaceRoot, "Linked"), nestedOutsideRoot).FullName;

        _directLinkError = await FailureFrom(workspaceRoot, Path.Combine(linkedRoot, "Outside.cs"), "Outside");
        _parentTraversalError = await FailureFrom(workspaceRoot, Path.Combine(linkedRoot, "..", "Escaped.cs"), "Escaped");
    }

    [Fact]
    void should_reject_the_directly_linked_document()
    {
        if (_isApplicable)
        {
            _directLinkError.ShouldBeOfExactType<InvalidScreenplayProjectSource>();
        }
    }

    [Fact]
    void should_reject_parent_traversal_after_the_link()
    {
        if (_isApplicable)
        {
            _parentTraversalError.ShouldBeOfExactType<InvalidScreenplayProjectSource>();
        }
    }

    void Destroy()
    {
        _workspace.Dispose();
        if (!string.IsNullOrEmpty(_fixtureRoot) && Directory.Exists(_fixtureRoot))
        {
            Directory.Delete(_fixtureRoot, recursive: true);
        }
    }

    async Task<Exception> FailureFrom(string workspaceRoot, string documentPath, string recordName)
    {
        var project = _workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            recordName,
            recordName,
            LanguageNames.CSharp,
            filePath: Path.Combine(workspaceRoot, $"{recordName}.csproj")));
        var solution = project.Solution.AddDocument(
            DocumentId.CreateNewId(project.Id),
            $"{recordName}.cs",
            SourceText.From($"public record {recordName};"),
            ["Links"],
            documentPath);
        project = solution.GetProject(project.Id)!;
        var compilation = await project.GetCompilationAsync() ?? throw new SourceFixtureCompilationFailed();

        return await Catch.Exception(() => ScreenplayProjectSources.Create(
            project,
            compilation,
            workspaceRoot,
            isSolution: true,
            CancellationToken.None));
    }
}
