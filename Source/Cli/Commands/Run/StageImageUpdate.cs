// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The outcome of asking whether the local Stage image should be, or was, updated.
/// </summary>
/// <param name="PreviousVersion">The newest tag that was present locally before this ran.</param>
/// <param name="CurrentVersion">The newest tag present locally after this ran - equal to <paramref name="PreviousVersion"/> when nothing changed.</param>
/// <param name="Updated">True when a newer tag was pulled.</param>
public sealed record StageImageUpdateResult(string PreviousVersion, string CurrentVersion, bool Updated);

/// <summary>
/// Checks whether a newer Stage image has been published, and pulls it - but only when a Stage image already
/// exists on this computer. The image is large and only <c language="csharp">cratis run</c> ever asks Docker
/// to pull one in the first place, so checking for everyone regardless would report on, and offer to fetch,
/// something most users never asked for.
/// </summary>
public static class StageImageUpdate
{
    /// <summary>
    /// Reads the newest Stage image tag Docker already has locally.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The newest tag present, or null when Docker is not installed, not reachable, or no Stage image has ever been pulled.</returns>
    public static async Task<string?> GetLatestLocalVersion(CancellationToken cancellationToken = default) =>
        StageImage.LatestVersionFrom(await DockerCli.Run(StageImage.BuildListTagsArguments(), cancellationToken));

    /// <summary>
    /// Checks whether a newer Stage image than the newest one already on this computer has been published.
    /// </summary>
    /// <param name="bypassCache">Whether to ask NuGet directly rather than trusting a cached answer.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The latest version string if newer than what is present locally, otherwise null - including
    /// when no Stage image is present at all, which this never reports on.</returns>
    /// <remarks>
    /// This is <see cref="UpdateChecker.CheckForUpdate(string, string, bool, CancellationToken)"/> given the
    /// newest local tag as "the current version" and <see cref="UpdateChecker.StagePackageId"/> as the NuGet
    /// package the Stage image shares its release version with - the same caching, the same NuGet lookup, the
    /// same freshness rules every other package already gets, not a second implementation of any of it.
    /// </remarks>
    public static async Task<string?> CheckForUpdate(bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        var localVersion = await GetLatestLocalVersion(cancellationToken);
        return localVersion is null
            ? null
            : await UpdateChecker.CheckForUpdate(UpdateChecker.StagePackageId, localVersion, bypassCache, cancellationToken);
    }

    /// <summary>
    /// Checks for a newer Stage image and pulls it when one exists, but only when a Stage image is already
    /// present locally.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The outcome, or null when no Stage image is present locally - the caller's cue to say nothing
    /// about the Stage image at all, rather than reporting on one that was never opted into.</returns>
    public static async Task<StageImageUpdateResult?> CheckAndUpdate(CancellationToken cancellationToken = default)
    {
        var localVersion = await GetLatestLocalVersion(cancellationToken);
        if (localVersion is null)
        {
            return null;
        }

        // Asking for an update is the user saying the cached answer is not good enough, so this always goes
        // to NuGet - same reason the CLI's own self-update check bypasses the cache too.
        var latest = await UpdateChecker.CheckForUpdate(UpdateChecker.StagePackageId, localVersion, true, cancellationToken);
        if (latest is null)
        {
            return new(localVersion, localVersion, false);
        }

        var pulled = await Pull(latest, cancellationToken);
        return pulled
            ? new(localVersion, latest, true)
            : new(localVersion, localVersion, false);
    }

    /// <summary>
    /// Pulls the given Stage image tag.
    /// </summary>
    /// <param name="tag">The tag to pull.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True when Docker reported success.</returns>
    public static async Task<bool> Pull(string tag, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in StageImage.BuildPullArguments(tag))
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
