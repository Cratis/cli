// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Resolves the folder the interpret command reads capture (<c>.jsonl</c>) files from.
/// </summary>
public static class CaptureFolders
{
    /// <summary>
    /// Resolves the capture folder — the path given on the command line when there is one, otherwise the
    /// configured JSON output directory when a <c>cratis-prologue.json</c> with JSON output was found, and the
    /// current directory as the last resort.
    /// </summary>
    /// <param name="path">The capture folder given on the command line; <see langword="null"/> when not given.</param>
    /// <param name="configuration">The configuration found for the run; <see langword="null"/> when none exists.</param>
    /// <param name="configurationPath">The full path of the found configuration file; <see langword="null"/> when none exists.</param>
    /// <param name="currentDirectory">The directory relative paths are resolved against.</param>
    /// <returns>The full path of the folder to read captures from.</returns>
    public static string Resolve(string? path, PrologueConfiguration? configuration, string? configurationPath, string currentDirectory)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return Path.GetFullPath(path, currentDirectory);
        }

        if (configuration?.Prologue.Output.Kind == OutputKind.Json &&
            configuration.Prologue.Output.Json.Directory is { Length: > 0 } directory &&
            configurationPath is not null)
        {
            return Path.GetFullPath(directory, Path.GetDirectoryName(configurationPath)!);
        }

        return currentDirectory;
    }
}
