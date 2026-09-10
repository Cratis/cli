// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Resolves the solution or project file a Screenplay is generated from.
/// </summary>
/// <remarks>
/// A solution filter is read as the solution it filters — naming a subset of a large repository is exactly how one
/// application within it is pointed at. It is discovered after the solutions themselves, because a folder holding
/// both is holding the whole application and one view of it.
/// </remarks>
public static class ScreenplayTargetResolver
{
    static readonly string[] _extensions = [".slnx", ".sln", ".slnf", ".csproj"];
    static readonly string[] _solutionExtensions = [".slnx", ".sln", ".slnf"];

    /// <summary>
    /// Gets the file extensions that identify a solution or project the generator can read, in discovery order.
    /// </summary>
    public static IReadOnlyList<string> Extensions => _extensions;

    /// <summary>
    /// Resolves the solution or project file to read.
    /// </summary>
    /// <param name="path">The path given on the command line — a solution file, a project file, or a folder. <see langword="null"/> uses <paramref name="currentDirectory"/>.</param>
    /// <param name="currentDirectory">The directory relative paths are resolved against and discovery starts from.</param>
    /// <returns>The <see cref="ScreenplayTarget"/> describing the outcome.</returns>
    public static ScreenplayTarget Resolve(string? path, string currentDirectory)
    {
        var candidate = string.IsNullOrWhiteSpace(path)
            ? currentDirectory
            : Path.GetFullPath(path, currentDirectory);

        if (File.Exists(candidate))
        {
            return IsSupportedFile(candidate)
                ? ScreenplayTarget.Resolved(candidate)
                : ScreenplayTarget.Unresolved(
                    $"'{candidate}' is not a solution or project file",
                    $"Point the command at a {string.Join(", ", _extensions)} file, or at the folder holding one");
        }

        if (!Directory.Exists(candidate))
        {
            return ScreenplayTarget.Unresolved(
                $"'{candidate}' does not exist",
                "Point the command at an existing solution file, project file, or folder");
        }

        return Discover(candidate);
    }

    /// <summary>
    /// Determines whether the given file is a solution or project file the generator can read.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns><see langword="true"/> when the file is supported.</returns>
    public static bool IsSupportedFile(string path) =>
        _extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the given file is a solution file rather than a project file.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns><see langword="true"/> when the file is a solution.</returns>
    public static bool IsSolution(string path) =>
        _solutionExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    static ScreenplayTarget Discover(string directory)
    {
        var current = new DirectoryInfo(directory);
        while (current is not null)
        {
            var found = FindIn(current);
            if (found is not null)
            {
                return found;
            }

            current = current.Parent;
        }

        return ScreenplayTarget.Unresolved(
            $"No solution or project file found in '{directory}' or any parent folder",
            $"Run the command from a folder holding a {string.Join(", ", _extensions)} file, or pass the path to one");
    }

    static ScreenplayTarget? FindIn(DirectoryInfo directory)
    {
        foreach (var extension in _extensions)
        {
            var matches = directory
                .GetFiles($"*{extension}", SearchOption.TopDirectoryOnly)
                .Select(file => file.FullName)
                .Where(file => string.Equals(Path.GetExtension(file), extension, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .ToArray();

            if (matches.Length == 1)
            {
                return ScreenplayTarget.Resolved(matches[0]);
            }

            if (matches.Length > 1)
            {
                return ScreenplayTarget.Unresolved(
                    $"Found {matches.Length} {extension} files in '{directory.FullName}'",
                    $"Pass the one to read: {string.Join(", ", matches.Select(Path.GetFileName))}");
            }
        }

        return null;
    }
}
