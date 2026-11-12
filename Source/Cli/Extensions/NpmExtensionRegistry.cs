// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Reads frontend-only extension packages from an npm registry. The package ships <c>contract.play</c> at its root.
/// </summary>
/// <remarks>
/// The registry signs every package over <c>name@version:integrity</c> with a key it publishes at <c>/-/npm/v1/keys</c>; the registry
/// therefore vouches for the key, and the publisher is the package's scope (or its name when it has none).
/// </remarks>
/// <param name="http">The authenticating <see cref="RegistryHttp"/>.</param>
/// <param name="baseUrl">The registry URL.</param>
public class NpmExtensionRegistry(RegistryHttp http, string baseUrl = NpmExtensionRegistry.DefaultRegistry) : IExtensionRegistry
{
    /// <summary>The public npm registry.</summary>
    public const string DefaultRegistry = "https://registry.npmjs.org";

    /// <summary>The name of the part holding the package tarball.</summary>
    public const string TarballPart = "package.tgz";

    /// <summary>
    /// Gets the npm package name of a full reference.
    /// </summary>
    /// <param name="resolved">The full reference, <c>npm:&lt;name&gt;</c>.</param>
    /// <returns>The package name.</returns>
    public static string PackageName(string resolved) => resolved.StartsWith("npm:", StringComparison.Ordinal) ? resolved[4..] : resolved;

    /// <summary>
    /// Gets the publisher identity of a package.
    /// </summary>
    /// <param name="name">The package name.</param>
    /// <returns><c>npm:&lt;scope&gt;</c>, or <c>npm:&lt;name&gt;</c> for an unscoped package.</returns>
    public static string PublisherOf(string name) => "npm:" + (name.StartsWith('@') ? name[..name.IndexOf('/')] : name);

    string Host => new Uri(baseUrl).Authority;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> Versions(string resolved, CancellationToken cancellationToken)
    {
        using var packument = await Packument(resolved, cancellationToken);
        return packument is null || !packument.RootElement.TryGetProperty("versions", out var versions) ? [] : [.. versions.EnumerateObject().Select(version => version.Name)];
    }

    /// <inheritdoc/>
    public async Task<RegistryPackage?> Pull(string resolved, string version, CancellationToken cancellationToken)
    {
        using var packument = await Packument(resolved, cancellationToken);
        if (packument is null || !packument.RootElement.GetProperty("versions").TryGetProperty(version, out var entry)) return null;
        var dist = entry.GetProperty("dist");
        var integrity = dist.TryGetProperty("integrity", out var value) ? value.GetString() : null;
        var tarball = await Download(resolved, dist.GetProperty("tarball").GetString()!, cancellationToken);
        if (integrity is null || integrity != "sha512-" + Convert.ToBase64String(SHA512.HashData(tarball)))
        {
            throw new ExtensionFailure(ExtensionFailureKind.Mismatched, resolved, $"The tarball of '{resolved}' {version} does not match the integrity the registry published.");
        }

        var contract = ContractIn(tarball) ?? throw new ExtensionFailure(ExtensionFailureKind.Mismatched, resolved, $"'{resolved}' {version} holds no contract.play at its root.");
        var digest = "sha256:" + Convert.ToHexString(SHA256.HashData(tarball)).ToLowerInvariant();
        return new(ExtensionPackageKind.Npm, resolved, version, digest, contract, [new(TarballPart, "application/gzip", tarball)], await Signature(resolved, version, integrity, dist, cancellationToken));
    }

    static byte[]? ContractIn(byte[] tarball)
    {
        try
        {
            using var raw = new MemoryStream(tarball);
            using var gzip = new GZipStream(raw, CompressionMode.Decompress);
            using var reader = new TarReader(gzip);
            while (reader.GetNextEntry() is { } entry)
            {
                if (entry.DataStream is null || entry.Name.TrimStart('.', '/') != "package/contract.play") continue;
                using var copy = new MemoryStream();
                entry.DataStream.CopyTo(copy);
                return copy.ToArray();
            }
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            return null;
        }

        return null;
    }

    async Task<JsonDocument?> Packument(string resolved, CancellationToken cancellationToken)
    {
        using var response = await http.Send(Host, resolved, () => new(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/{PackageName(resolved).Replace("/", "%2F")}"), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    async Task<byte[]> Download(string resolved, string url, CancellationToken cancellationToken)
    {
        using var response = await http.Send(new Uri(url).Authority, resolved, () => new(HttpMethod.Get, url), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    async Task<SignatureEvidence?> Signature(string resolved, string version, string integrity, JsonElement dist, CancellationToken cancellationToken)
    {
        if (!dist.TryGetProperty("signatures", out var signatures) || signatures.GetArrayLength() == 0) return null;
        using var response = await http.Send(Host, resolved, () => new(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/-/npm/v1/keys"), cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        using var keys = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var published = keys.RootElement.GetProperty("keys").EnumerateArray().ToDictionary(key => key.GetProperty("keyid").GetString()!, key => key.GetProperty("key").GetString()!);
        foreach (var signature in signatures.EnumerateArray())
        {
            if (!published.TryGetValue(signature.GetProperty("keyid").GetString()!, out var key)) continue;
            var name = PackageName(resolved);
            return new(PublisherOf(name), SignatureAnchor.Registry, Convert.FromBase64String(key), Encoding.UTF8.GetBytes($"{name}@{version}:{integrity}"), Convert.FromBase64String(signature.GetProperty("sig").GetString()!));
        }

        return null;
    }
}
