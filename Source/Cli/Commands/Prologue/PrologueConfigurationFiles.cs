// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Resolves where <c>cratis-prologue.json</c> files live for the prologue commands — where the setup wizard
/// writes a new one, and where an existing one is found for interpretation.
/// </summary>
public static class PrologueConfigurationFiles
{
    /// <summary>
    /// Resolves the path the setup wizard writes the configuration file to.
    /// </summary>
    /// <param name="file">The path given on the command line — a file, an existing directory, or <see langword="null"/> for the default.</param>
    /// <param name="currentDirectory">The directory relative paths are resolved against.</param>
    /// <returns>The full path of the configuration file to write.</returns>
    public static string ResolveOutputPath(string? file, string currentDirectory)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return Path.Combine(currentDirectory, PrologueConfigurationFile.FileName);
        }

        var full = Path.GetFullPath(file, currentDirectory);
        if (Directory.Exists(full) || file.EndsWith(Path.DirectorySeparatorChar) || file.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return Path.Combine(full, PrologueConfigurationFile.FileName);
        }

        return full;
    }

    /// <summary>
    /// Finds an existing <c>cratis-prologue.json</c> for interpretation — in the given capture folder first,
    /// then in the current directory.
    /// </summary>
    /// <param name="path">The capture folder given on the command line; <see langword="null"/> when not given.</param>
    /// <param name="currentDirectory">The directory relative paths are resolved against.</param>
    /// <returns>The full path of the configuration file, or <see langword="null"/> when none exists.</returns>
    public static string? Find(string? path, string currentDirectory)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(path))
        {
            candidates.Add(Path.Combine(Path.GetFullPath(path, currentDirectory), PrologueConfigurationFile.FileName));
        }

        candidates.Add(Path.Combine(currentDirectory, PrologueConfigurationFile.FileName));

        return candidates.FirstOrDefault(File.Exists);
    }
}
