// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Screenplay;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Resolves the path the interpret command writes the generated Screenplay (<c>.play</c>) file to.
/// </summary>
public static class ScreenplayOutput
{
    /// <summary>
    /// Resolves the output path — the file given on the command line when there is one, otherwise a file named
    /// after the interpreted system (through <see cref="ScreenplayFileName"/>) in the current directory.
    /// </summary>
    /// <param name="file">The output file given on the command line; <see langword="null"/> when not given.</param>
    /// <param name="systemName">The name derived for the interpreted system.</param>
    /// <param name="currentDirectory">The directory relative paths are resolved against.</param>
    /// <returns>The full path of the Screenplay file to write.</returns>
    public static string ResolvePath(string? file, string systemName, string currentDirectory) =>
        string.IsNullOrWhiteSpace(file)
            ? Path.Combine(currentDirectory, ScreenplayFileName.For(systemName))
            : Path.GetFullPath(file, currentDirectory);
}
