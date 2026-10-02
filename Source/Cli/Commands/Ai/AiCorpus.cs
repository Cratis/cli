// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>A resolved Cratis AI corpus. A corpus this CLI downloaded is deleted on dispose; a supplied checkout never is.</summary>
/// <param name="Path">The corpus checkout path.</param>
/// <param name="Downloaded">Whether this CLI downloaded the checkout, and so owns it.</param>
public sealed class AiCorpus(string Path, bool Downloaded) : IDisposable
{
    /// <summary>Gets the corpus checkout path.</summary>
    public string Path { get; } = Path;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Downloaded) DeleteFolder(Path);
    }

    internal static void DeleteFolder(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return;

            // Git writes its pack files read-only, which a recursive delete refuses on Windows.
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(path, true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A leftover temp folder must not turn a finished run into a failure.
        }
    }
}
