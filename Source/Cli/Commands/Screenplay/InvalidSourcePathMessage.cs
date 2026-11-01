// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Builds the <c language="csharp">CLI0017</c> message so it names why a source path was refused and which path it was.
/// </summary>
/// <remarks>
/// Diagnostics are portable output and never carry a machine's physical roots, so a physical path is named relative to
/// the directory of the solution or project that was read - the same path for every checkout with the same layout. A path
/// whose only shared ancestor with that directory is the filesystem root has no such portable form and is named by its
/// file name alone. A logical path is already portable and is named as it is.
/// </remarks>
static class InvalidSourcePathMessage
{
    /// <summary>
    /// Builds the message for a project whose source paths cannot be mapped.
    /// </summary>
    /// <param name="projectName">The project the path belongs to.</param>
    /// <param name="exception">The mapping failure.</param>
    /// <param name="targetPath">The full path of the solution or project that was read.</param>
    /// <returns>The message, naming the reason and, when one path is responsible, that path.</returns>
    internal static string For(string projectName, InvalidScreenplayProjectSource exception, string targetPath) =>
        $"Source paths for project '{projectName}' cannot be mapped to stable portable identities: {Describe(exception, targetPath)}";

    /// <summary>
    /// Builds the message for a direct project-reference closure that cannot be mapped.
    /// </summary>
    /// <param name="exception">The mapping failure.</param>
    /// <param name="targetPath">The full path of the project that was read.</param>
    /// <returns>The message, naming the reason and, when one path is responsible, that path.</returns>
    internal static string ForClosure(InvalidScreenplayProjectSource exception, string targetPath) =>
        $"The direct project-reference closure cannot be mapped to a trusted workspace boundary: {Describe(exception, targetPath)}";

    static string Describe(InvalidScreenplayProjectSource exception, string targetPath) =>
        exception.Path is { Length: > 0 } path
            ? $"{exception.Message} ('{Portable(path, targetPath)}')"
            : exception.Message;

    static string Portable(string path, string targetPath)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            return path.Replace('\\', '/');
        }

        var anchor = Path.GetDirectoryName(Path.GetFullPath(targetPath));
        if (anchor is null)
        {
            return Path.GetFileName(path);
        }

        var relative = Path.GetRelativePath(anchor, path);
        var parentSteps = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TakeWhile(part => part == "..").Count();
        var anchorDepth = anchor[Path.GetPathRoot(anchor)!.Length..]
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Length;
        return Path.IsPathFullyQualified(relative) || parentSteps >= anchorDepth
            ? Path.GetFileName(path)
            : relative.Replace('\\', '/');
    }
}
