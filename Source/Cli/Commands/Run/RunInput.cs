// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The admitted Screenplay file or folder, or the reason it cannot be run.
/// </summary>
/// <param name="Target">The existing file or folder to mount, when admitted.</param>
/// <param name="Error">The validation error, when the input cannot be admitted.</param>
internal sealed record RunInput(FileSystemInfo? Target, string? Error)
{
    /// <summary>
    /// Resolves and classifies an input without searching outside it or starting Docker.
    /// </summary>
    /// <param name="path">The file or folder path to resolve.</param>
    /// <returns>The admitted input or its validation error.</returns>
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

        // Docker's -v syntax uses colons as delimiters. Preserve native Windows drive prefixes, but
        // do not reinterpret or attempt to escape colons in host file or folder names. Commas are literal here.
        var source = OperatingSystem.IsWindows() && path.Length > 2 && path[1] == ':' ? path[2..] : path;
        if (source.Contains(':', StringComparison.Ordinal))
        {
            return new(null, "The Screenplay file or folder path contains a colon that Docker's -v syntax cannot represent");
        }

        if (File.Exists(path))
        {
            return string.Equals(Path.GetExtension(path), ".play", StringComparison.OrdinalIgnoreCase)
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
