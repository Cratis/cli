// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Creates stable authored-source metadata for projects loaded through a Roslyn workspace.
/// </summary>
static class ScreenplayProjectSources
{
    /// <summary>
    /// Gets the platform-appropriate comparer for physical project paths.
    /// </summary>
    internal static StringComparer PhysicalPathComparer { get; } =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// Creates the authored-tree set and source metadata for one project compilation.
    /// </summary>
    /// <param name="project">The workspace project.</param>
    /// <param name="compilation">The final compilation adapters will analyze.</param>
    /// <param name="workspaceRoot">The physical root used only while deriving relative paths.</param>
    /// <param name="usesWorkspaceDisplayRoot">Whether source display paths are workspace-relative.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The aligned authored trees and source metadata.</returns>
    /// <exception cref="InvalidScreenplayProjectSource">Thrown when source paths cannot be mapped safely.</exception>
    internal static async Task<(IReadOnlySet<SyntaxTree> AuthoredSyntaxTrees, ScreenplayProjectSource Source)> Create(
        Project project,
        Compilation compilation,
        string workspaceRoot,
        bool usesWorkspaceDisplayRoot,
        CancellationToken cancellationToken)
    {
        try
        {
            var projectPath = FullyQualified(project.FilePath);
            var root = FullyQualified(workspaceRoot);
            var logicalProjectPath = RelativeTo(root, projectPath);
            var documents = new List<DotNetSourceDocument>();
            var authoredSyntaxTrees = new HashSet<SyntaxTree>();
            var packageContentFiles = NuGetPackageContentFiles.From(project);

            foreach (var document in project.Documents)
            {
                var documentPath = FullyQualified(document.FilePath);
                if (packageContentFiles.Contains(documentPath))
                {
                    continue;
                }

                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken) ??
                    throw new InvalidScreenplayProjectSource("An authored document did not map to a syntax tree");
                if (!compilation.SyntaxTrees.Contains(syntaxTree))
                {
                    throw new InvalidScreenplayProjectSource("An authored syntax tree was not present in the project compilation");
                }

                authoredSyntaxTrees.Add(syntaxTree);
                documents.Add(new DotNetSourceDocument
                {
                    SyntaxTree = syntaxTree,
                    ProjectRelativePath = ProjectRelativePathOf(document),
                    WorkspaceRelativePath = RelativeTo(root, documentPath)
                });
            }

            var policy = new DotNetSourcePathPolicy
            {
                DisplayRoot = usesWorkspaceDisplayRoot ? DotNetSourceDisplayRoot.Workspace : DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            };
            var identity = Path.ChangeExtension(logicalProjectPath, null)?.Replace('\\', '/') ?? logicalProjectPath;
            var context = DotNetSourcePaths.Create(identity, policy, documents);
            if (context.Files.Count != authoredSyntaxTrees.Count || authoredSyntaxTrees.Any(tree => !context.Files.ContainsKey(tree)))
            {
                throw new InvalidScreenplayProjectSource("An authored syntax tree was not mapped by the source context");
            }

            return (authoredSyntaxTrees, new ScreenplayProjectSource(projectPath, logicalProjectPath, context)
            {
                Role = DotNetProjectRole.Application,
                SourceRoot = root
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidScreenplayProjectSource)
        {
            throw;
        }
        catch (Exception exception) when (IsSourcePathFailure(exception))
        {
            throw new InvalidScreenplayProjectSource("The project contains a source path that cannot be represented safely", exception);
        }
    }

    /// <summary>
    /// Produces a portable relative path and rejects anything outside the declared workspace root.
    /// </summary>
    /// <param name="root">The physical workspace root.</param>
    /// <param name="path">The physical path.</param>
    /// <returns>The portable path beneath the root.</returns>
    /// <exception cref="InvalidScreenplayProjectSource">Thrown when the path is outside the root.</exception>
    internal static string RelativeTo(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        var canonicalRelative = Path.GetRelativePath(CanonicalPathOf(root), CanonicalPathOf(path)).Replace('\\', '/');
        if (IsOutside(canonicalRelative))
        {
            throw new InvalidScreenplayProjectSource("A workspace source path is outside the declared display root");
        }

        return IsOutside(relative) ? canonicalRelative : relative;
    }

    /// <summary>
    /// Resolves existing symbolic-link path components without requiring the complete path to be a link itself.
    /// </summary>
    /// <param name="path">The fully qualified physical path.</param>
    /// <returns>The canonical physical path.</returns>
    /// <exception cref="InvalidScreenplayProjectSource">Thrown when the path cannot be canonicalized safely.</exception>
    internal static string CanonicalPathOf(string path)
    {
        try
        {
            return ResolveCanonicalPathOf(FullyQualified(path));
        }
        catch (InvalidScreenplayProjectSource)
        {
            throw;
        }
        catch (Exception exception) when (IsSourcePathFailure(exception))
        {
            throw new InvalidScreenplayProjectSource("A physical project path cannot be canonicalized safely", exception);
        }
    }

    /// <summary>
    /// Builds the project-relative identity path from Roslyn's logical folders and document name.
    /// </summary>
    /// <param name="document">The workspace document.</param>
    /// <returns>The logical project-relative path.</returns>
    static string ProjectRelativePathOf(Document document) =>
        string.Join('/', document.Folders.Append(document.Name).Select(PortablePart));

    /// <summary>
    /// Ensures a logical document path part cannot introduce a root or traversal.
    /// </summary>
    /// <param name="part">The logical path part.</param>
    /// <returns>The validated path part.</returns>
    /// <exception cref="InvalidScreenplayProjectSource">Thrown when the path part is malformed.</exception>
    static string PortablePart(string part)
    {
        if (string.IsNullOrWhiteSpace(part) ||
            string.Equals(part, ".", StringComparison.Ordinal) ||
            string.Equals(part, "..", StringComparison.Ordinal) ||
            Path.IsPathFullyQualified(part) ||
            part.Contains('/') ||
            part.Contains('\\') ||
            part.Any(char.IsControl))
        {
            throw new InvalidScreenplayProjectSource("A logical document path is rooted, traversing, or malformed");
        }

        return part;
    }

    /// <summary>
    /// Ensures a physical path is present and fully qualified.
    /// </summary>
    /// <param name="path">The physical path.</param>
    /// <returns>The validated fully qualified path.</returns>
    /// <exception cref="InvalidScreenplayProjectSource">Thrown when the path is missing or relative.</exception>
    static string FullyQualified(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            throw new InvalidScreenplayProjectSource("A workspace source path is missing or is not fully qualified");
        }

        return path;
    }

    static string ResolveCanonicalPathOf(string path)
    {
        var root = Path.GetPathRoot(path)!;
        var current = root;
        foreach (var part in path[root.Length..].Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(part, ".", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(part, "..", StringComparison.Ordinal))
            {
                current = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(current)) ?? root;
                continue;
            }

            current = Path.Combine(current, part);
            FileSystemInfo fileSystemInfo = Directory.Exists(current)
                ? new DirectoryInfo(current)
                : new FileInfo(current);
            if (fileSystemInfo.Exists && fileSystemInfo.ResolveLinkTarget(returnFinalTarget: true) is { } target)
            {
                current = target.FullName;
            }
        }

        return Path.GetFullPath(current);
    }

    static bool IsOutside(string relative) =>
        Path.IsPathFullyQualified(relative) ||
        string.Equals(relative, ".", StringComparison.Ordinal) ||
        string.Equals(relative, "..", StringComparison.Ordinal) ||
        relative.StartsWith("../", StringComparison.Ordinal);

    /// <summary>
    /// Determines whether an exception represents a source-path contract failure.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns><see langword="true"/> when the exception represents a source-path contract failure.</returns>
    static bool IsSourcePathFailure(Exception exception) =>
        exception is InvalidDotNetSourcePath or
            InvalidDotNetProjectIdentity or
            UnsupportedDotNetSourcePathPolicy or
            DuplicateDotNetSourceIdentity or
            DuplicateDotNetSourceTree or
            DotNetSourceTreeNotMapped or
            ArgumentException or
            IOException or
            NotSupportedException or
            UnauthorizedAccessException;
}

/// <summary>
/// The exception that is thrown when workspace source paths cannot be represented safely.
/// </summary>
sealed class InvalidScreenplayProjectSource : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidScreenplayProjectSource"/> class.
    /// </summary>
    /// <param name="message">The failure description.</param>
    internal InvalidScreenplayProjectSource(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidScreenplayProjectSource"/> class.
    /// </summary>
    /// <param name="message">The failure description.</param>
    /// <param name="innerException">The source-path contract failure.</param>
    internal InvalidScreenplayProjectSource(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
