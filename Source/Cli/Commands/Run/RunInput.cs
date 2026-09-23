// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The Screenplay file or folder a run hands to the Stage container, or the reason it cannot be run.
/// </summary>
/// <param name="Target">The existing file or folder to mount, when the input is admitted.</param>
/// <param name="Error">Why the input cannot be run, when it is not admitted.</param>
public sealed record RunInput(FileSystemInfo? Target, string? Error)
{
    /// <summary>
    /// The file extension that identifies a Screenplay file.
    /// </summary>
    public const string Extension = ".play";

    /// <summary>
    /// Resolves a path to the Screenplay file or folder it names, without looking outside it.
    /// </summary>
    /// <param name="path">The file or folder path to resolve, absolute or relative to the current directory.</param>
    /// <returns>The admitted input, or the reason it cannot be run.</returns>
    /// <remarks>
    /// A file must have the <c language="csharp">.play</c> extension in any casing, and a folder must hold at least one
    /// Screenplay file somewhere beneath it. Nothing ever falls back to the parent of what was asked for - a file
    /// that is not a Screenplay, or one that is missing, is an error even when its siblings are Screenplay files.
    /// <para>
    /// Docker's <c language="csharp">-v</c> option separates the host path from the container path with a colon, so a
    /// colon in the host path cannot be represented and is rejected here rather than escaped or reinterpreted. A
    /// Windows drive prefix is not part of that restriction.
    /// </para>
    /// </remarks>
    public static RunInput Resolve(string path)
    {
        try
        {
            path = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new(null, "The Screenplay file or folder path is invalid");
        }

        var source = OperatingSystem.IsWindows() && path.Length > 2 && path[1] == ':'
            ? path[2..]
            : path;

        if (source.Contains(':', StringComparison.Ordinal))
        {
            return new(null, $"'{path}' contains a colon, which Docker's -v option cannot represent");
        }

        if (File.Exists(path))
        {
            return string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase)
                ? new(new FileInfo(path), null)
                : new(null, $"'{path}' is not a Screenplay (.play) file");
        }

        if (!Directory.Exists(path))
        {
            return new(null, $"File or folder '{path}' does not exist");
        }

        return PlayFiles.ExistIn(path)
            ? new(new DirectoryInfo(path), null)
            : new(null, "No Screenplay files (.play) found in the folder");
    }
}
