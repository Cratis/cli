// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Recognizes documents a restored NuGet package contributes to a project rather than the application authoring.
/// </summary>
/// <remarks>
/// A package reaches a compilation in two ways: as <c language="csharp">contentFiles</c>, and through the
/// <c language="csharp">build</c> targets it imports, which may add <c language="csharp">Compile</c> items from
/// inside the package — <c language="csharp">Microsoft.NET.Test.Sdk</c> adds its generated entry point this way to
/// any project that references it. Both live in the package's own directory under a restored package folder, never
/// beside the application, so they are not authored source and have no portable identity to give.
/// </remarks>
/// <param name="ContentFiles">The restored <c language="csharp">contentFiles</c> paths.</param>
/// <param name="PackageDirectories">The directories of the restored packages, each ending in a separator.</param>
sealed record RestoredPackageDocuments(IReadOnlySet<string> ContentFiles, IReadOnlyList<string> PackageDirectories)
{
    static readonly StringComparison _pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    /// <summary>
    /// Gets the package documents restored for a project.
    /// </summary>
    /// <param name="project">The workspace project.</param>
    /// <returns>The package documents, ignoring any package directory the project itself sits inside.</returns>
    internal static RestoredPackageDocuments From(Project project)
    {
        var assetsFile = ProjectRestoreState.AssetsFileFor(project.FilePath, project.CompilationOutputInfo.AssemblyPath);
        var documents = From(assetsFile);
        if (string.IsNullOrWhiteSpace(project.FilePath) || !Path.IsPathFullyQualified(project.FilePath))
        {
            return documents;
        }

        var projectPath = Path.GetFullPath(project.FilePath);
        return documents with
        {
            PackageDirectories = [.. documents.PackageDirectories.Where(directory => !IsBeneath(directory, projectPath))]
        };
    }

    /// <summary>
    /// Gets the package documents described by a NuGet assets file.
    /// </summary>
    /// <param name="assetsFile">The NuGet assets file.</param>
    /// <returns>The package documents; empty when the assets file is absent or unreadable.</returns>
    internal static RestoredPackageDocuments From(string? assetsFile) =>
        new(NuGetPackageContentFiles.From(assetsFile), NuGetPackageContentFiles.PackageDirectoriesFrom(assetsFile));

    /// <summary>
    /// Determines whether a document belongs to a restored package.
    /// </summary>
    /// <param name="path">The fully qualified document path.</param>
    /// <returns><see langword="true"/> when the document is package content or sits inside a package directory.</returns>
    internal bool Owns(string path)
    {
        if (ContentFiles.Contains(path))
        {
            return true;
        }

        var fullPath = Path.GetFullPath(path);
        return PackageDirectories.Any(directory => IsBeneath(directory, fullPath));
    }

    static bool IsBeneath(string directory, string path) => path.StartsWith(directory, _pathComparison);
}
