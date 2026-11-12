// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Reads extension packages from, and copies them to, an OCI registry through the distribution API.
/// </summary>
/// <remarks>
/// A package is the artifact at <c language="csharp">host/repository:version</c>. Its contract is a layer titled <c language="csharp">contract.play</c>, or the file
/// <c language="csharp">extension/contract.play</c> inside a tar layer. Its signature is a cosign-layout artifact tagged <c language="csharp">sha256-&lt;manifest hex&gt;.sig</c>.
/// </remarks>
/// <param name="http">The authenticating <see cref="RegistryHttp"/>.</param>
/// <param name="prefix">A repository prefix when the registry mirrors packages beneath one; empty to use the reference's own repository.</param>
/// <param name="hostOverride">The registry host when it mirrors packages from other registries; <see langword="null"/> to use the reference's own host.</param>
public partial class OciExtensionRegistry(RegistryHttp http, string prefix = "", string? hostOverride = null) : IExtensionRegistry, IExtensionDestination
{
    const string ManifestAccept = "application/vnd.oci.image.manifest.v1+json, application/vnd.docker.distribution.manifest.v2+json";
    const string SignatureMediaType = "application/vnd.dev.cosign.simplesigning.v1+json";
    const string SignatureAnnotation = "dev.cosignproject.cosign/signature";

    /// <inheritdoc/>
    public string Description => $"oci://{hostOverride}/{prefix}".TrimEnd('/');

    [GeneratedRegex("^\\d+(\\.\\d+){0,2}([-+][0-9A-Za-z.-]+)?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VersionTag { get; }

    [GeneratedRegex("<(?<url>[^>]+)>;\\s*rel=\"next\"", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NextLink { get; }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> Versions(string resolved, CancellationToken cancellationToken)
    {
        var (host, repository) = Locate(resolved);
        var versions = new List<string>();
        var next = (string?)$"/v2/{repository}/tags/list";
        while (next is not null)
        {
            using var response = await http.Send(host, resolved, () => new(HttpMethod.Get, Uri(host, next)), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return [];
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            {
                versions.AddRange(tags.EnumerateArray().Select(tag => tag.GetString()).OfType<string>().Where(tag => VersionTag.IsMatch(tag)));
            }

            next = NextPage(response);
        }

        return versions;
    }

    /// <inheritdoc/>
    public async Task<RegistryPackage?> Pull(string resolved, string version, CancellationToken cancellationToken)
    {
        var (host, repository) = Locate(resolved);
        var manifest = await Get(host, resolved, $"/v2/{repository}/manifests/{version}", ManifestAccept, cancellationToken);
        if (manifest is null) return null;
        var digest = Digest(manifest);
        using var document = JsonDocument.Parse(manifest);
        var parts = new List<ArtifactPart> { new("manifest.json", "application/vnd.oci.image.manifest.v1+json", manifest) };
        byte[]? contract = null;
        foreach (var descriptor in Descriptors(document.RootElement))
        {
            var blob = await Get(host, resolved, $"/v2/{repository}/blobs/{descriptor.Digest}", "*/*", cancellationToken)
                ?? throw new ExtensionFailure(ExtensionFailureKind.Unresolvable, resolved, $"{host} does not hold the blob {descriptor.Digest} of '{resolved}' {version}.");
            if (Digest(blob) != descriptor.Digest) throw new ExtensionFailure(ExtensionFailureKind.Mismatched, resolved, $"{host} served bytes for {descriptor.Digest} that do not match it.");
            parts.Add(new($"blob-{descriptor.Digest.Replace("sha256:", string.Empty)}", descriptor.MediaType, blob));
            if (descriptor.IsLayer) contract ??= ContractIn(blob, descriptor);
        }

        if (contract is null) throw new ExtensionFailure(ExtensionFailureKind.Mismatched, resolved, $"'{resolved}' {version} holds no extension/contract.play.");
        return new(ExtensionPackageKind.Oci, resolved, version, digest, contract, parts, await Signature(host, resolved, repository, digest, cancellationToken));
    }

    /// <inheritdoc/>
    public async Task Push(RegistryPackage package, CancellationToken cancellationToken)
    {
        if (package.Kind != ExtensionPackageKind.Oci) throw new ExtensionFailure(ExtensionFailureKind.Unresolvable, package.Resolved, $"'{package.Resolved}' is an npm package; vendor it to a folder.");
        var (host, repository) = Locate(package.Resolved);
        foreach (var part in package.Parts.Where(part => part.Name != "manifest.json")) await PutBlob(host, package.Resolved, repository, part.Content, cancellationToken);
        var manifest = package.Parts.First(part => part.Name == "manifest.json").Content;
        await PutManifest(host, package.Resolved, repository, package.Version, "application/vnd.oci.image.manifest.v1+json", manifest, cancellationToken);
        if (package.Signature is null) return;
        var config = "{}"u8.ToArray();
        await PutBlob(host, package.Resolved, repository, config, cancellationToken);
        await PutBlob(host, package.Resolved, repository, package.Signature.Payload, cancellationToken);
        var signature = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = 2,
            mediaType = "application/vnd.oci.image.manifest.v1+json",
            config = new { mediaType = "application/vnd.oci.image.config.v1+json", digest = Digest(config), size = config.Length },
            layers = new[]
            {
                new
                {
                    mediaType = SignatureMediaType,
                    digest = Digest(package.Signature.Payload),
                    size = package.Signature.Payload.Length,
                    annotations = new Dictionary<string, string> { [SignatureAnnotation] = Convert.ToBase64String(package.Signature.Signature) }
                }
            }
        });
        await PutManifest(host, package.Resolved, repository, SignatureTag(package.Digest), "application/vnd.oci.image.manifest.v1+json", signature, cancellationToken);
    }

    static HttpRequestMessage Accepting(HttpRequestMessage request, string accept)
    {
        request.Headers.TryAddWithoutValidation("Accept", accept);
        return request;
    }

    static string? NextPage(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var links)) return null;
        var match = NextLink.Match(links.First());
        return match.Success ? match.Groups["url"].Value : null;
    }

    static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    static string SignatureTag(string digest) => digest.Replace(':', '-') + ".sig";

    static string Uri(string host, string path) => $"{(IsLocal(host) ? "http" : "https")}://{host}{path}";

    static bool IsLocal(string host) => host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) || host.StartsWith("127.0.0.1", StringComparison.Ordinal);

    static IEnumerable<Descriptor> Descriptors(JsonElement manifest)
    {
        if (manifest.TryGetProperty("config", out var config)) yield return Describe(config, false);
        if (!manifest.TryGetProperty("layers", out var layers)) yield break;
        foreach (var layer in layers.EnumerateArray()) yield return Describe(layer, true);
    }

    static Descriptor Describe(JsonElement element, bool isLayer)
    {
        var title = element.TryGetProperty("annotations", out var annotations) && annotations.TryGetProperty("org.opencontainers.image.title", out var value) ? value.GetString() : null;
        return new(element.GetProperty("digest").GetString()!, element.GetProperty("mediaType").GetString() ?? "application/octet-stream", title, isLayer);
    }

    static byte[]? ContractIn(byte[] blob, Descriptor descriptor)
    {
        if (descriptor.Title == "contract.play") return blob;
        try
        {
            using var raw = new MemoryStream(blob);
            using var stream = blob.Length > 2 && blob[0] == 0x1f && blob[1] == 0x8b ? new GZipStream(raw, CompressionMode.Decompress) : (Stream)raw;
            using var reader = new TarReader(stream);
            while (reader.GetNextEntry() is { } entry)
            {
                if (entry.DataStream is not null && entry.Name.TrimStart('.', '/') == "extension/contract.play")
                {
                    using var copy = new MemoryStream();
                    entry.DataStream.CopyTo(copy);
                    return copy.ToArray();
                }
            }
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            return null;
        }

        return null;
    }

    (string Host, string Repository) Locate(string resolved)
    {
        var slash = resolved.IndexOf('/');
        var host = resolved[..slash];
        var repository = resolved[(slash + 1)..];
        return (hostOverride ?? host, prefix.Length == 0 ? repository : $"{prefix.Trim('/')}/{repository}");
    }

    async Task<byte[]?> Get(string host, string reference, string path, string accept, CancellationToken cancellationToken)
    {
        using var response = await http.Send(host, reference, () => Accepting(new(HttpMethod.Get, Uri(host, path)), accept), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    async Task<SignatureEvidence?> Signature(string host, string reference, string repository, string digest, CancellationToken cancellationToken)
    {
        var manifest = await Get(host, reference, $"/v2/{repository}/manifests/{SignatureTag(digest)}", ManifestAccept, cancellationToken);
        if (manifest is null) return null;
        using var document = JsonDocument.Parse(manifest);
        var layer = document.RootElement.GetProperty("layers").EnumerateArray().FirstOrDefault();
        if (layer.ValueKind != JsonValueKind.Object || !layer.TryGetProperty("annotations", out var annotations) || !annotations.TryGetProperty(SignatureAnnotation, out var encoded)) return null;
        var payload = await Get(host, reference, $"/v2/{repository}/blobs/{layer.GetProperty("digest").GetString()}", "*/*", cancellationToken);
        if (payload is null) return null;
        try
        {
            using var simple = JsonDocument.Parse(payload);
            var optional = simple.RootElement.GetProperty("optional");
            return new(optional.GetProperty("publisher").GetString()!, SignatureAnchor.Embedded, Convert.FromBase64String(optional.GetProperty("key").GetString()!), payload, Convert.FromBase64String(encoded.GetString()!));
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or FormatException or InvalidOperationException)
        {
            throw new ExtensionFailure(ExtensionFailureKind.SignatureInvalid, reference, $"The signature of '{reference}' does not name its publisher and key.");
        }
    }

    async Task PutBlob(string host, string reference, string repository, byte[] content, CancellationToken cancellationToken)
    {
        var digest = Digest(content);
        using var start = await http.Send(host, reference, () => new(HttpMethod.Post, Uri(host, $"/v2/{repository}/blobs/uploads/")), cancellationToken);
        start.EnsureSuccessStatusCode();
        var location = start.Headers.Location!.IsAbsoluteUri ? start.Headers.Location.ToString() : Uri(host, start.Headers.Location.ToString());
        var target = location + (location.Contains('?') ? "&" : "?") + "digest=" + System.Uri.EscapeDataString(digest);
        using var done = await http.Send(host, reference, () => new(HttpMethod.Put, target) { Content = new ByteArrayContent(content) { Headers = { ContentType = new("application/octet-stream") } } }, cancellationToken);
        done.EnsureSuccessStatusCode();
    }

    async Task PutManifest(string host, string reference, string repository, string tag, string mediaType, byte[] manifest, CancellationToken cancellationToken)
    {
        using var response = await http.Send(host, reference, () => new(HttpMethod.Put, Uri(host, $"/v2/{repository}/manifests/{tag}")) { Content = new ByteArrayContent(manifest) { Headers = { ContentType = new(mediaType) } } }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    sealed record Descriptor(string Digest, string MediaType, string? Title, bool IsLayer);
}
