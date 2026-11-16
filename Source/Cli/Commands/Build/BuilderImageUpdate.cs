// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A newer version of a packaged builder image that is on this computer.
/// </summary>
/// <param name="Repository">The repository of the image.</param>
/// <param name="Version">The newer version published on Docker Hub.</param>
public sealed record BuilderImageVersion(string Repository, string Version);

/// <summary>
/// The outcome of asking whether a local builder image should be, or was, updated.
/// </summary>
/// <param name="Repository">The repository of the image.</param>
/// <param name="PreviousVersion">The newest version that was present locally before this ran.</param>
/// <param name="CurrentVersion">The newest version present locally after this ran - equal to <paramref name="PreviousVersion"/> when nothing changed.</param>
/// <param name="Updated">True when a newer version was pulled.</param>
public sealed record BuilderImageUpdateResult(string Repository, string PreviousVersion, string CurrentVersion, bool Updated);

/// <summary>
/// Checks whether newer versions of the packaged builder images have been published, and pulls them - but only for
/// the images already on this computer. The images are large and only <c language="csharp">cratis build run --docker</c>
/// ever pulls one, so checking for everyone would report on images nobody asked for.
/// </summary>
public static class BuilderImageUpdate
{
    /// <summary>
    /// Gets the hint shown after a command when a newer builder image is out.
    /// </summary>
    /// <param name="available">The newer version.</param>
    /// <returns>The hint.</returns>
    public static string HintFor(BuilderImageVersion available) =>
        $"Builder image update available: {available.Repository} {available.Version} - run 'cratis update'";

    /// <summary>
    /// Describes an update outcome the way <c language="csharp">cratis update</c> reports it.
    /// </summary>
    /// <param name="result">The outcome.</param>
    /// <returns>The description.</returns>
    public static string Describe(BuilderImageUpdateResult result) =>
        result.Updated
            ? $"Updated the builder image {result.Repository} from {result.PreviousVersion} to {result.CurrentVersion}"
            : $"The builder image {result.Repository} is already at the latest version ({result.PreviousVersion})";

    /// <summary>
    /// Checks whether newer versions of the builder images on this computer have been published.
    /// </summary>
    /// <param name="bypassCache">Whether to ask Docker Hub directly rather than trusting a cached answer.</param>
    /// <param name="refreshes">Where to register the background refreshes, so the caller can wait for them; null when nobody waits.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The newer versions; empty when there are none, no builder image is present, or the check is switched off.</returns>
    public static async Task<IReadOnlyList<BuilderImageVersion>> CheckForUpdate(bool bypassCache, VersionRefreshes? refreshes, CancellationToken cancellationToken = default)
    {
        // Switching the update check off switches off the local Docker inspection that only exists to feed it.
        if (UpdateChecker.IsDisabled())
        {
            return [];
        }

        var local = await BuilderImage.GetLatestLocalVersions(BuilderImage.Repositories, cancellationToken);
        var checks = local.Select(async entry =>
            await UpdateChecker.CheckForDockerHubUpdate(entry.Key, entry.Value, bypassCache, refreshes, cancellationToken) is { } latest
                ? new BuilderImageVersion(entry.Key, latest)
                : null);
        return [.. (await Task.WhenAll(checks)).OfType<BuilderImageVersion>()];
    }

    /// <summary>
    /// Pulls the newest version of every builder image on this computer that has a newer one published.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>One outcome per builder image present; empty when none is - the caller's cue to say nothing about them.</returns>
    public static async Task<IReadOnlyList<BuilderImageUpdateResult>> CheckAndUpdate(CancellationToken cancellationToken = default)
    {
        var local = await BuilderImage.GetLatestLocalVersions(BuilderImage.Repositories, cancellationToken);

        // Asking for an update is the user saying the cached answer is not good enough, so this always goes to Docker Hub.
        return await Update(
            local,
            (repository, version) => UpdateChecker.CheckForDockerHubUpdate(repository, version, true, null, cancellationToken),
            async reference => (await DockerCli.Execute(["pull", reference], cancellationToken)).ExitCode == 0);
    }

    /// <summary>
    /// Updates each local builder image whose repository has published a newer version.
    /// </summary>
    /// <param name="local">The newest local version of each builder image present.</param>
    /// <param name="newerVersion">Reads the newer published version of a repository given its local version, or null when there is none.</param>
    /// <param name="pull">Pulls an image reference and tells whether it succeeded.</param>
    /// <returns>One outcome per image.</returns>
    internal static async Task<IReadOnlyList<BuilderImageUpdateResult>> Update(
        IReadOnlyDictionary<string, string> local,
        Func<string, string, Task<string?>> newerVersion,
        Func<string, Task<bool>> pull)
    {
        var results = new List<BuilderImageUpdateResult>();
        foreach (var (repository, version) in local.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            var latest = await newerVersion(repository, version);
            var updated = latest is not null && await pull($"{repository}:{latest}");
            results.Add(new(repository, version, updated ? latest! : version, updated));
        }

        return results;
    }
}
