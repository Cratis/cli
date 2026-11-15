// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Finds a kit by what a person names: a folder on disk, a Git URL (optionally followed by <c language="csharp">#</c> and a
/// branch, tag or commit), or the name of a kit Studio registers. A registered kit's archive is verified against its
/// checksum before it is unpacked; every kit is validated before anything is copied into a project.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="studio">The Studio address registered kits are read from, or <see langword="null"/>.</param>
/// <param name="scratch">The folder kits are cloned or unpacked into, outside the project.</param>
public sealed partial class BuildKitSource(HttpClient http, Uri? studio, string scratch)
{
    /// <summary>The response header holding the SHA-256 of a kit archive.</summary>
    public const string ChecksumHeader = "X-Checksum-Sha256";

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex KitName { get; }

    /// <summary>
    /// Gets a value indicating whether a reference names a Git repository.
    /// </summary>
    /// <param name="reference">The reference.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    public static bool IsGit(string reference) =>
        reference.StartsWith("git@", StringComparison.Ordinal) ||
        reference.StartsWith("ssh://", StringComparison.Ordinal) ||
        reference.StartsWith("file://", StringComparison.Ordinal) ||
        ((reference.StartsWith("https://", StringComparison.Ordinal) || reference.StartsWith("http://", StringComparison.Ordinal)) && !reference.Contains("/api/builders/kits/", StringComparison.Ordinal));

    /// <summary>
    /// Fetches a kit and loads it.
    /// </summary>
    /// <param name="reference">A folder, a Git URL with an optional <c language="csharp">#ref</c>, or a registered kit's name.</param>
    /// <param name="folder">The folder inside a Git repository that holds the manifest, or <see langword="null"/> for its root.</param>
    /// <param name="cancellationToken">Cancels the fetch.</param>
    /// <returns>The validated kit and where it came from.</returns>
    /// <exception cref="InvalidBuildKit">The kit cannot be found, fetched or validated.</exception>
    public async Task<BuildKitFetched> Fetch(string reference, string? folder, CancellationToken cancellationToken)
    {
        if (Directory.Exists(reference)) return new(BuildKit.Load(InFolder(Path.GetFullPath(reference), folder)), null, Path.GetFullPath(reference), null);
        if (IsGit(reference)) return await Clone(reference, folder, cancellationToken);
        if (KitName.IsMatch(reference)) return await Download(reference, cancellationToken);
        throw new InvalidBuildKit([$"(document): '{reference}' is not a folder, a Git URL or the name of a registered kit."]);
    }

    static string InFolder(string root, string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return root;
        var path = Path.GetFullPath(Path.Combine(root, folder.Trim('/')));
        if (!path.StartsWith(root, StringComparison.Ordinal) || !Directory.Exists(path))
        {
            throw new InvalidBuildKit([$"(document): the kit has no folder {folder}."]);
        }

        return path;
    }

    static async Task Git(string folder, string[] arguments, string url, CancellationToken cancellationToken)
    {
        var result = await BuildProcess.Run("git", arguments, folder, cancellationToken: cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidBuildKit([$"(document): the kit could not be fetched from {BuildRedaction.Patterns.Redact(url)}: {BuildRedaction.Patterns.Redact(result.Error.Trim())}"]);
        }
    }

    async Task<BuildKitFetched> Clone(string reference, string? folder, CancellationToken cancellationToken)
    {
        var hash = reference.LastIndexOf('#');
        var url = hash < 0 ? reference : reference[..hash];
        var gitRef = hash < 0 ? "HEAD" : reference[(hash + 1)..];
        var target = Path.Combine(scratch, $"git-{Guid.NewGuid():N}");
        Directory.CreateDirectory(target);

        await Git(target, ["init", "--quiet"], url, cancellationToken);
        await Git(target, ["fetch", "--quiet", "--depth", "1", url, gitRef], url, cancellationToken);
        await Git(target, ["checkout", "--quiet", "FETCH_HEAD"], url, cancellationToken);
        var commit = (await BuildProcess.Run("git", ["rev-parse", "HEAD"], target, cancellationToken: cancellationToken)).Output.Trim();

        return new(BuildKit.Load(InFolder(Path.GetFullPath(target), folder)), null, BuildRedaction.Patterns.Redact(reference), commit);
    }

    async Task<BuildKitFetched> Download(string name, CancellationToken cancellationToken)
    {
        if (studio is null)
        {
            throw new InvalidBuildKit([$"(document): '{name}' names a registered kit, but no Studio is known. Pass --studio, or connect with 'cratis build connect'."]);
        }

        using var response = await http.GetAsync(new Uri(studio, $"/api/builders/kits/{Uri.EscapeDataString(name)}/archive"), cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidBuildKit([$"(document): Studio registers no kit named {name} ({(int)response.StatusCode})."]);

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var expected = response.Headers.TryGetValues(ChecksumHeader, out var values) ? values.FirstOrDefault()?.ToLowerInvariant() : null;
        var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (expected is null || !string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidBuildKit([$"(document): the archive of {name} does not match its checksum; it was not used."]);
        }

        var target = Path.Combine(scratch, $"kit-{name}-{Guid.NewGuid():N}");
        ScreenplaySessionBundle.Unpack(bytes, target);
        var root = BuildKitManifestText.Find(target) is null && Directory.GetDirectories(target) is [var single] && Directory.GetFiles(target).Length == 0 ? single : target;
        return new(BuildKit.Load(root), actual, null, null);
    }
}
