// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Discovers Screenplay (<c language="csharp">.play</c>) files within a folder tree.
/// </summary>
public static class PlayFiles
{
    /// <summary>
    /// The glob pattern that identifies a Screenplay file.
    /// </summary>
    public const string SearchPattern = "*.play";

    /// <summary>
    /// Determines whether the given folder contains at least one Screenplay (<c language="csharp">.play</c>) file,
    /// searching recursively through all subfolders.
    /// </summary>
    /// <param name="path">The folder to search.</param>
    /// <returns>True if one or more <c language="csharp">.play</c> files are present; otherwise false.</returns>
    public static bool ExistIn(string path) =>
        Directory.Exists(path) &&
        Directory.EnumerateFiles(path, SearchPattern, SearchOption.AllDirectories).Any();
}
