// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Fetches the Screenplay session bundle, verifies its checksum and unpacks it into a cache outside every repository. Studio
/// names the version it pins; without a Studio the newest release of Cratis/AI is used.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="cacheRoot">The cache folder; each verified bundle is kept under its checksum.</param>
public sealed class ScreenplaySessionBundle(HttpClient http, string cacheRoot)
{
    /// <summary>The address of the newest released bundle.</summary>
    public static readonly Uri LatestRelease = new("https://github.com/Cratis/AI/releases/latest/download/screenplay-session.zip");

    /// <summary>
    /// Gets the default cache folder.
    /// </summary>
    public static string DefaultCache => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cratis", "cache", "screenplay-session");

    /// <summary>
    /// Unpacks an archive into a folder, refusing any entry that would leave it.
    /// </summary>
    /// <param name="bytes">The archive.</param>
    /// <param name="folder">The folder.</param>
    /// <exception cref="InvalidDataException">An entry would leave the folder.</exception>
    public static void Unpack(byte[] bytes, string folder)
    {
        var staging = $"{folder}.{Guid.NewGuid():N}.partial";
        Directory.CreateDirectory(staging);
        try
        {
            using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
            {
                var root = Path.GetFullPath(staging) + Path.DirectorySeparatorChar;
                foreach (var entry in zip.Entries)
                {
                    var target = Path.GetFullPath(Path.Combine(staging, entry.FullName));
                    if (!target.StartsWith(root, StringComparison.Ordinal)) throw new InvalidDataException($"The archive entry {entry.FullName} leaves the archive.");
                    if (entry.FullName.EndsWith('/')) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    entry.ExtractToFile(target, overwrite: true);
                }
            }

            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            Directory.Move(staging, folder);
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    /// <summary>
    /// Resolves the guidance Studio pins, or the newest release when there is no Studio or it names none.
    /// </summary>
    /// <param name="studio">The Studio address, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>The verified, unpacked guidance.</returns>
    /// <exception cref="InvalidDataException">The bundle does not match its checksum, or is not a bundle this host reads.</exception>
    public async Task<ScreenplaySessionGuidance> Resolve(Uri? studio, CancellationToken cancellationToken)
    {
        var (archive, checksum) = (studio is null ? null : await FromStudio(studio, cancellationToken)) ?? await FromRelease(cancellationToken);
        var folder = Path.Combine(cacheRoot, checksum);
        if (File.Exists(Path.Combine(folder, "bundle.json"))) return ScreenplaySessionGuidance.Load(folder);

        var bytes = await http.GetByteArrayAsync(archive, cancellationToken);
        var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (!string.Equals(actual, checksum, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The Screenplay session bundle from {archive} does not match its published checksum; it was not used.");
        }

        Unpack(bytes, folder);
        return ScreenplaySessionGuidance.Load(folder);
    }

    async Task<(Uri Archive, string Checksum)?> FromStudio(Uri studio, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri(studio, "/api/builders/session"), cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var guidance = (JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken)) as JsonObject)?["guidance"];
        var archive = guidance?["archive"]?.GetValue<string>();
        var checksum = guidance?["sha256"]?.GetValue<string>();
        return archive is null || checksum is null ? null : (new Uri(studio, archive), checksum.ToLowerInvariant());
    }

    async Task<(Uri Archive, string Checksum)> FromRelease(CancellationToken cancellationToken)
    {
        var line = await http.GetStringAsync(new Uri($"{LatestRelease}.sha256"), cancellationToken);
        var checksum = line.Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant();
        return checksum is { Length: 64 } ? (LatestRelease, checksum) : throw new InvalidDataException("The published checksum of the Screenplay session bundle cannot be read.");
    }
}
