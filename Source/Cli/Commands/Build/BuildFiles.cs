// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// File operations the host needs for kits and workspaces.
/// </summary>
public static class BuildFiles
{
    /// <summary>
    /// Copies a folder and everything in it.
    /// </summary>
    /// <param name="source">The folder to copy.</param>
    /// <param name="destination">Where to copy it to.</param>
    /// <param name="overwrite">Whether a file already at the destination is replaced.</param>
    /// <returns>The files copied, relative to the destination.</returns>
    public static IReadOnlyList<string> CopyFolder(string source, string destination, bool overwrite = true)
    {
        var copied = new List<string>();
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            if (!overwrite && File.Exists(target)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite);
            copied.Add(relative.Replace('\\', '/'));
        }

        return copied;
    }

    /// <summary>
    /// Writes a Screenplay document of an order into a workspace, refusing a path that would leave it.
    /// </summary>
    /// <param name="root">The folder the documents go in.</param>
    /// <param name="document">The document.</param>
    /// <exception cref="InvalidBuildOrder">The document's path leaves the folder.</exception>
    public static void WriteDocument(string root, BuildOrderDocument document)
    {
        var full = Path.GetFullPath(Path.Combine(root, document.Path));
        if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidBuildOrder($"The Screenplay document {document.Path} leaves the model.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, document.Content);
    }
}
