// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Acquires only named Cratis Screenplay release assets, verifying every cached/downloaded byte.</summary>
/// <param name="http">Release source HTTP transport.</param>
/// <param name="platform">Native platform and cache root.</param>
internal sealed partial class DesktopMcpArtifacts(HttpClient http, DesktopMcpPlatform platform)
{
    const string ReleaseSource = "https://github.com/Cratis/Screenplay/releases/download";

    [GeneratedRegex(@"\A(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex VersionPattern { get; }

    internal static void ValidateVersion(string version)
    {
        if (!VersionPattern.IsMatch(version)) throw new AiMcpConfigurationInvalid("Expected a Screenplay semantic version, for example 4.55.0.");
    }

    internal static bool HasUpdate(string? latest, string installed) => latest is not null &&
        System.Version.TryParse(latest.Split('-')[0], out var candidate) &&
        System.Version.TryParse(installed.Split('-')[0], out var current) &&
        (candidate > current || (candidate == current && installed.Contains('-') && !latest.Contains('-')));

    internal static async Task Verify(string path, string expected, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        if (!string.Equals(hash, expected, StringComparison.OrdinalIgnoreCase)) throw new AiMcpConfigurationInvalid("Screenplay artifact integrity check failed. Remove the cached download and retry; do not install it.");
    }

    internal async Task<string> Latest()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var response = await http.GetAsync("https://api.github.com/repos/Cratis/Screenplay/releases/latest", deadline.Token);
        LatestVersion.ThrowIfRateLimited(response, "Cratis/Screenplay");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
        var version = document.RootElement.GetProperty("tag_name").GetString()!.TrimStart('v');
        ValidateVersion(version);
        return version;
    }

    internal string Name(string version, string suffix)
    {
        ValidateVersion(version);
        if (platform.Rid is null || suffix is not (".mcpb" or "-plugin.zip")) throw new AiMcpConfigurationInvalid("No Screenplay desktop artifact for this platform.");
        return $"screenplay-{version}-{platform.Rid}{suffix}";
    }

    internal async Task<string> Acquire(string version, string suffix)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var name = Name(version, suffix);
        var folder = AiProjectPaths.Within(platform.Home, ".cratis/mcp-desktop/downloads");
        var destination = AiProjectPaths.Within(platform.Home, $".cratis/mcp-desktop/downloads/{name}");
        var url = $"{ReleaseSource}/v{version}/{name}";

        // Checksums come from the same publisher-owned HTTPS release, not from a caller-supplied URL.
        var checksum = await http.GetStringAsync($"{url}.sha256", deadline.Token);
        var parts = checksum.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || parts[1] != name || parts[0].Length != 64 || !parts[0].All(Uri.IsHexDigit))
            throw new AiMcpConfigurationInvalid("Malformed Screenplay artifact checksum.");
        if (File.Exists(destination))
        {
            await Verify(destination, parts[0], deadline.Token);
            return destination;
        }
        Directory.CreateDirectory(folder);
        var temporary = Path.Combine(folder, $"{Guid.NewGuid():N}.tmp");
        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > 512L * 1024 * 1024) throw new AiMcpConfigurationInvalid("Screenplay artifact is too large.");
            await using (var source = await response.Content.ReadAsStreamAsync(deadline.Token))
            await using (var target = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, deadline.Token)) > 0)
                {
                    total += read;
                    if (total > 512L * 1024 * 1024) throw new AiMcpConfigurationInvalid("Screenplay artifact is too large.");
                    await target.WriteAsync(buffer.AsMemory(0, read), deadline.Token);
                }
            }
            await Verify(temporary, parts[0], deadline.Token);
            File.Move(temporary, destination);
            return destination;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
