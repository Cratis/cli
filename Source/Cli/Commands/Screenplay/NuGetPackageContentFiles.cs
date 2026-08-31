// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Finds restored documents that NuGet <c>contentFiles</c> packages may contribute to a project.
/// </summary>
static class NuGetPackageContentFiles
{
    static readonly StringComparer _pathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    /// <summary>
    /// Gets the physical package content files restored for a project.
    /// </summary>
    /// <param name="project">The workspace project.</param>
    /// <returns>The fully qualified paths of package-owned document candidates.</returns>
    internal static IReadOnlySet<string> From(Project project) =>
        From(ProjectRestoreState.AssetsFileFor(project.FilePath, project.CompilationOutputInfo.AssemblyPath));

    /// <summary>
    /// Gets the physical package content files described by a NuGet assets file.
    /// </summary>
    /// <param name="assetsFile">The NuGet assets file.</param>
    /// <returns>The fully qualified paths of package-owned document candidates.</returns>
    internal static IReadOnlySet<string> From(string? assetsFile)
    {
        if (assetsFile is null)
        {
            return new HashSet<string>(_pathComparer);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(assetsFile));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("packageFolders", out var packageFolders) ||
                packageFolders.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("libraries", out var libraries) ||
                libraries.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("targets", out var targets) ||
                targets.ValueKind != JsonValueKind.Object)
            {
                return new HashSet<string>(_pathComparer);
            }

            var folders = packageFolders.EnumerateObject()
                .Select(folder => folder.Name)
                .Where(Path.IsPathFullyQualified)
                .Select(Path.GetFullPath)
                .ToArray();
            var files = new HashSet<string>(_pathComparer);
            foreach (var target in targets.EnumerateObject().Where(target => target.Value.ValueKind == JsonValueKind.Object))
            {
                AddFrom(target.Value, libraries, folders, files);
            }

            return files;
        }
        catch (JsonException)
        {
            return new HashSet<string>(_pathComparer);
        }
        catch (IOException)
        {
            return new HashSet<string>(_pathComparer);
        }
        catch (UnauthorizedAccessException)
        {
            return new HashSet<string>(_pathComparer);
        }
    }

    /// <summary>
    /// Adds package content files from one restored target.
    /// </summary>
    /// <param name="target">The restored target.</param>
    /// <param name="libraries">The restored library metadata.</param>
    /// <param name="packageFolders">The configured package cache roots.</param>
    /// <param name="files">The paths to populate.</param>
    static void AddFrom(
        JsonElement target,
        JsonElement libraries,
        IReadOnlyList<string> packageFolders,
        HashSet<string> files)
    {
        foreach (var package in target.EnumerateObject())
        {
            if (package.Value.ValueKind != JsonValueKind.Object ||
                !package.Value.TryGetProperty("type", out var type) ||
                type.ValueKind != JsonValueKind.String ||
                !string.Equals(type.GetString(), "package", StringComparison.OrdinalIgnoreCase) ||
                !package.Value.TryGetProperty("contentFiles", out var contentFiles) ||
                contentFiles.ValueKind != JsonValueKind.Object ||
                !libraries.TryGetProperty(package.Name, out var library) ||
                library.ValueKind != JsonValueKind.Object ||
                !library.TryGetProperty("path", out var libraryPathElement) ||
                libraryPathElement.ValueKind != JsonValueKind.String ||
                !SafeRelativePath(libraryPathElement.GetString(), out var libraryPath))
            {
                continue;
            }

            foreach (var contentFile in contentFiles.EnumerateObject())
            {
                if (contentFile.Value.ValueKind != JsonValueKind.Object ||
                    !SafeRelativePath(contentFile.Name, out var contentPath))
                {
                    continue;
                }

                foreach (var packageFolder in packageFolders)
                {
                    var path = Path.GetFullPath(Path.Combine(packageFolder, libraryPath, contentPath));
                    if (File.Exists(path))
                    {
                        files.Add(path);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates and converts an assets-file path to the current platform's separators.
    /// </summary>
    /// <param name="path">The assets-file path.</param>
    /// <param name="safePath">The validated relative path.</param>
    /// <returns><see langword="true"/> when the path is relative and cannot traverse.</returns>
    static bool SafeRelativePath(string? path, out string safePath)
    {
        safePath = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathFullyQualified(path))
        {
            return false;
        }

        var parts = path.Replace('\\', '/').Split('/');
        if (parts.Any(part =>
                string.IsNullOrWhiteSpace(part) ||
                string.Equals(part, ".", StringComparison.Ordinal) ||
                string.Equals(part, "..", StringComparison.Ordinal) ||
                part.Any(char.IsControl)))
        {
            return false;
        }

        safePath = Path.Combine(parts);
        return true;
    }
}
