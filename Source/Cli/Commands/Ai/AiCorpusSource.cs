// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Obtains the canonical AI repository when a local checkout was not supplied.</summary>
public static class AiCorpusSource
{
    const string Repository = "https://github.com/Cratis/AI.git";

    /// <summary>Clones the current default branch into a temporary directory.</summary>
    /// <returns>The temporary checkout path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Git cannot start or clone the corpus.</exception>
    public static string Download()
    {
        var destination = Path.Combine(Path.GetTempPath(), "cratis-ai", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var process = Process.Start(new ProcessStartInfo("git", $"clone --depth 1 {Repository} \"{destination}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("Could not start git to download the Cratis AI corpus.");
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException($"Could not download the Cratis AI corpus: {process.StandardError.ReadToEnd()}");
        return destination;
    }
}
