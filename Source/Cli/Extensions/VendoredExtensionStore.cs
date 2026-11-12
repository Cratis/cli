// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Reads and writes packages vendored into a folder laid out <c language="csharp">&lt;digest with ':' as '-'&gt;/</c>, holding <c language="csharp">contract.extension</c>
/// (the same file the Screenplay compiler reads from its <c language="csharp">extensions</c> folder), <c language="csharp">package.json</c> and the artifact <c language="csharp">parts/</c>.
/// </summary>
/// <param name="root">The folder.</param>
public class VendoredExtensionStore(string root) : IExtensionRegistry, IExtensionDestination
{
    /// <summary>The publisher recorded for a package without a signature of its own; never trusted on its own.</summary>
    public const string Publisher = "vendored";

    const string DescriptorFile = "package.json";
    const string ContractFile = "contract.extension";

    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <inheritdoc/>
    public string Description => root;

    /// <summary>
    /// Gets the folder of a digest.
    /// </summary>
    /// <param name="digest">The package digest.</param>
    /// <returns>The folder.</returns>
    public string FolderOf(string digest) => Path.Combine(root, digest.Replace(':', '-'));

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> Versions(string resolved, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([.. Descriptors().Where(descriptor => descriptor.Resolved == resolved).Select(descriptor => descriptor.Version)]);

    /// <inheritdoc/>
    public Task<RegistryPackage?> Pull(string resolved, string version, CancellationToken cancellationToken) =>
        Task.FromResult(Descriptors().FirstOrDefault(descriptor => descriptor.Resolved == resolved && descriptor.Version == version) is { } descriptor ? Read(descriptor.Digest) : null);

    /// <summary>
    /// Reads the package of a digest.
    /// </summary>
    /// <param name="digest">The package digest.</param>
    /// <returns>The package, or <see langword="null"/> when the folder does not hold it.</returns>
    public RegistryPackage? Read(string digest)
    {
        var folder = FolderOf(digest);
        var path = Path.Combine(folder, DescriptorFile);
        if (!ExtensionHashes.IsWellFormed(digest) || !File.Exists(path)) return null;
        var descriptor = JsonSerializer.Deserialize<VendoredDescriptor>(File.ReadAllText(path), _options)!;
        var contract = File.ReadAllBytes(Path.Combine(folder, ContractFile));
        var parts = descriptor.Parts.Select(part => new ArtifactPart(part.Name, part.MediaType, File.ReadAllBytes(Path.Combine(folder, "parts", Path.GetFileName(part.Name))))).ToArray();
        var signature = descriptor.Signature is null ? null : new SignatureEvidence(descriptor.Signature.Publisher, descriptor.Signature.Anchor, descriptor.Signature.PublicKey, descriptor.Signature.Payload, descriptor.Signature.Signature);
        return new(descriptor.Kind, descriptor.Resolved, descriptor.Version, descriptor.Digest, contract, parts, signature);
    }

    /// <inheritdoc/>
    public async Task Push(RegistryPackage package, CancellationToken cancellationToken)
    {
        var folder = FolderOf(package.Digest);
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        Directory.CreateDirectory(Path.Combine(folder, "parts"));
        await File.WriteAllBytesAsync(Path.Combine(folder, ContractFile), package.Contract, cancellationToken);
        foreach (var part in package.Parts) await File.WriteAllBytesAsync(Path.Combine(folder, "parts", Path.GetFileName(part.Name)), part.Content, cancellationToken);
        var signature = package.Signature is null ? null : new VendoredSignature(package.Signature.Publisher, package.Signature.Anchor, package.Signature.PublicKey, package.Signature.Payload, package.Signature.Signature);
        var descriptor = new VendoredDescriptor(package.Kind, package.Resolved, package.Version, package.Digest, [.. package.Parts.Select(part => new VendoredPart(part.Name, part.MediaType))], signature);
        await File.WriteAllTextAsync(Path.Combine(folder, DescriptorFile), JsonSerializer.Serialize(descriptor, _options) + "\n", cancellationToken);
    }

    IEnumerable<VendoredDescriptor> Descriptors() =>
        !Directory.Exists(root)
            ? []
            : Directory.GetDirectories(root)
                .Select(folder => Path.Combine(folder, DescriptorFile))
                .Where(File.Exists)
                .Select(path => JsonSerializer.Deserialize<VendoredDescriptor>(File.ReadAllText(path), _options)!);

    sealed record VendoredPart(string Name, string MediaType);

    sealed record VendoredSignature(string Publisher, SignatureAnchor Anchor, byte[] PublicKey, byte[] Payload, byte[] Signature);

    sealed record VendoredDescriptor(ExtensionPackageKind Kind, string Resolved, string Version, string Digest, IReadOnlyList<VendoredPart> Parts, VendoredSignature? Signature);
}
