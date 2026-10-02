// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Obtains the canonical AI repository when a local checkout was not supplied.</summary>
public static class AiCorpusSource
{
    const string Repository = "https://github.com/Cratis/AI.git";

    /// <summary>
    /// Resolves the corpus to read: the given local checkout when there is one, otherwise a fresh download.
    /// </summary>
    /// <param name="given">A local checkout supplied through <c language="csharp">--source</c> or <c language="csharp">CRATIS_AI_SOURCE</c>, if any.</param>
    /// <returns>The corpus. Dispose it when finished so a downloaded copy does not outlive the run.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a download is needed and Git cannot start or clone the corpus.</exception>
    public static AiCorpus Resolve(string? given) => Resolve(given, Download);

    /// <summary>Clones the current default branch into a temporary directory.</summary>
    /// <returns>The temporary checkout path. The caller owns it and should delete it, or use <see cref="Resolve(string?)"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Git cannot start or clone the corpus.</exception>
    public static string Download() => Download(Repository, Path.Combine(Path.GetTempPath(), "cratis-ai"));

    /// <summary>Resolves the corpus, downloading through the given function when no local checkout was supplied.</summary>
    /// <param name="given">A local checkout, if any. It is never deleted.</param>
    /// <param name="download">Downloads the corpus and returns its path. The returned folder is deleted on dispose.</param>
    /// <returns>The corpus.</returns>
    internal static AiCorpus Resolve(string? given, Func<string> download) =>
        given is null ? new AiCorpus(download(), true) : new AiCorpus(given, false);

    /// <summary>Clones the repository into a new folder under the given root; a failed clone leaves no folder behind.</summary>
    /// <param name="repository">The Git repository to clone.</param>
    /// <param name="root">The folder to create the checkout folder in.</param>
    /// <returns>The checkout path.</returns>
    internal static string Download(string repository, string root)
    {
        var destination = Path.Combine(root, Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            using var process = Process.Start(new ProcessStartInfo("git", $"clone --depth 1 \"{repository}\" \"{destination}\"")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            }) ?? throw new InvalidOperationException("Could not start git to download the Cratis AI corpus.");
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException($"Could not download the Cratis AI corpus: {process.StandardError.ReadToEnd()}");
            return destination;
        }
        catch
        {
            AiCorpus.DeleteFolder(destination);
            throw;
        }
    }
}
