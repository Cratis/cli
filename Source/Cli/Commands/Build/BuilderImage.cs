// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Knows the packaged builder images and picks the one to run: the repository for the render target, at the newest
/// version already on this computer, or else the newest version published on Docker Hub. The images are released
/// with Studio, so their version says nothing about this CLI's.
/// </summary>
public static class BuilderImage
{
    /// <summary>The image of the Cratis builder: .NET, Node.js and the Cratis CLI.</summary>
    public const string CratisRepository = "cratis/builder";

    /// <summary>The image of the Axon builder: the Cratis builder with what an Axon application needs.</summary>
    public const string AxonRepository = "cratis/studio-axon-builder";

    /// <summary>The image of the Event Modelers builder.</summary>
    public const string EventModelersRepository = "cratis/studio-eventmodelers-builder";

    /// <summary>Every packaged builder image Cratis publishes - the ones an update is looked for.</summary>
    public static readonly IReadOnlyList<string> Repositories = [CratisRepository, AxonRepository, EventModelersRepository];

    /// <summary>
    /// Gets the repository of the builder image for a render target.
    /// </summary>
    /// <param name="target">The render target, such as <c language="csharp">cratis</c> or <c language="csharp">axon</c>.</param>
    /// <returns>The repository, without a tag.</returns>
    public static string RepositoryFor(string target) =>
        string.Equals(target, BuildLevels.AxonTarget, StringComparison.OrdinalIgnoreCase) ? AxonRepository : CratisRepository;

    /// <summary>
    /// Tells whether an image reference names its version, with a tag or a digest.
    /// </summary>
    /// <param name="image">The image reference.</param>
    /// <returns>True when the reference names a tag or a digest.</returns>
    /// <remarks>
    /// A colon before the last slash belongs to a registry's port, as in <c language="csharp">localhost:5000/builder</c>, not to a tag.
    /// </remarks>
    public static bool HasVersion(string image) =>
        image.Contains('@') || image.LastIndexOf(':') > image.LastIndexOf('/');

    /// <summary>
    /// Builds the argument list for listing the packaged builder images Docker already has locally, one
    /// <c language="csharp">repository tag</c> pair per line.
    /// </summary>
    /// <param name="repositories">The repositories to list.</param>
    /// <returns>The ordered argument list to pass to the <c language="csharp">docker</c> executable.</returns>
    public static IReadOnlyList<string> BuildListArguments(IEnumerable<string> repositories) =>
        ["images", "--format", "{{.Repository}} {{.Tag}}", .. repositories.SelectMany(repository => new[] { "--filter", $"reference={repository}" })];

    /// <summary>
    /// Reads the newest version tag of each repository from what <see cref="BuildListArguments"/> answered.
    /// </summary>
    /// <param name="output">What Docker wrote.</param>
    /// <returns>The newest version of each repository that has one; a repository with only moving or locally built tags is left out.</returns>
    public static IReadOnlyDictionary<string, string> LatestVersionsFrom(string output) =>
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(' ', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .GroupBy(parts => parts[0], parts => parts[1], StringComparer.Ordinal)
            .Select(group => (Repository: group.Key, Version: StageImage.LatestVersionFrom(string.Join('\n', group))))
            .Where(entry => entry.Version is not null)
            .ToDictionary(entry => entry.Repository, entry => entry.Version!, StringComparer.Ordinal);

    /// <summary>
    /// Reads the newest version of each repository Docker already has locally.
    /// </summary>
    /// <param name="repositories">The repositories to look for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The newest version of each repository present; empty when Docker is not installed or not reachable.</returns>
    public static async Task<IReadOnlyDictionary<string, string>> GetLatestLocalVersions(IEnumerable<string> repositories, CancellationToken cancellationToken = default) =>
        LatestVersionsFrom(await DockerCli.Run(BuildListArguments(repositories), cancellationToken));

    /// <summary>
    /// Decides which image to run.
    /// </summary>
    /// <param name="image">The image asked for with <c language="csharp">--image</c>, or <see langword="null"/>.</param>
    /// <param name="target">The render target, which picks the repository when no image was asked for.</param>
    /// <param name="localVersion">Reads the newest version of a repository already on this computer, or null when there is none.</param>
    /// <param name="publishedVersion">Reads the newest version of a repository published on Docker Hub, or null when it could not be read.</param>
    /// <returns>The image to run, and whether it has to be pulled first.</returns>
    /// <exception cref="BuilderImageUnavailable">Nothing is on this computer and Docker Hub could not tell the newest version.</exception>
    public static async Task<BuilderImageChoice> Resolve(string? image, string target, Func<string, Task<string?>> localVersion, Func<string, Task<string?>> publishedVersion)
    {
        if (!string.IsNullOrWhiteSpace(image) && HasVersion(image))
        {
            return new(image, false);
        }

        var repository = string.IsNullOrWhiteSpace(image) ? RepositoryFor(target) : image;
        if (await localVersion(repository) is { } local)
        {
            return new($"{repository}:{local}", false);
        }

        var published = await publishedVersion(repository) ?? throw new BuilderImageUnavailable(repository);
        return new($"{repository}:{published}", true);
    }
}
