// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Resolves the Screenplay document, or the folder of documents, that is validated.
/// </summary>
/// <remarks>
/// Unlike <see cref="ScreenplayTargetResolver"/> nothing is discovered by searching upwards — a folder is a perfectly
/// good answer on its own, because every <c>.play</c> file beneath it is compiled.
/// </remarks>
public static class PlayFileTargetResolver
{
    /// <summary>
    /// The file extension that identifies a Screenplay document.
    /// </summary>
    public const string Extension = ".play";

    /// <summary>
    /// Resolves the document or folder to compile.
    /// </summary>
    /// <param name="path">The path given on the command line — a <c>.play</c> file or a folder. <see langword="null"/> uses <paramref name="currentDirectory"/>.</param>
    /// <param name="currentDirectory">The directory relative paths are resolved against.</param>
    /// <returns>The <see cref="ScreenplayTarget"/> describing the outcome.</returns>
    public static ScreenplayTarget Resolve(string? path, string currentDirectory)
    {
        var candidate = string.IsNullOrWhiteSpace(path)
            ? currentDirectory
            : Path.GetFullPath(path, currentDirectory);

        if (File.Exists(candidate))
        {
            return IsPlayFile(candidate)
                ? ScreenplayTarget.Resolved(candidate)
                : ScreenplayTarget.Unresolved(
                    $"'{candidate}' is not a Screenplay ({Extension}) file",
                    $"Point the command at a {Extension} file, or at the folder holding one");
        }

        return Directory.Exists(candidate)
            ? ScreenplayTarget.Resolved(candidate)
            : ScreenplayTarget.Unresolved(
                $"'{candidate}' does not exist",
                $"Point the command at an existing {Extension} file or folder");
    }

    /// <summary>
    /// Determines whether the given file is a Screenplay document.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns><see langword="true"/> when the file is a <c>.play</c> file.</returns>
    public static bool IsPlayFile(string path) =>
        string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase);
}
