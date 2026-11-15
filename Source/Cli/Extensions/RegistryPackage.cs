// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Represents the channel an extension package travels on.
/// </summary>
public enum ExtensionPackageKind
{
    /// <summary>An OCI artifact, the only way to ship a backend.</summary>
    Oci,

    /// <summary>An npm package, for frontend-only packages.</summary>
    Npm
}

/// <summary>
/// Represents one named blob of a package artifact, kept byte for byte so vendoring reproduces the artifact.
/// </summary>
/// <param name="Name">The part name, a plain file name.</param>
/// <param name="MediaType">The media type the registry served it with.</param>
/// <param name="Content">The bytes.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Package content is immutable bytes handed as-is to hashing, signature and file APIs.")]
public record ArtifactPart(string Name, string MediaType, byte[] Content);

/// <summary>
/// Represents a package pulled from a registry or a vendored folder, with everything needed to trust and copy it.
/// </summary>
/// <param name="Kind">The channel the package travels on.</param>
/// <param name="Resolved">The full reference.</param>
/// <param name="Version">The release.</param>
/// <param name="Digest">The digest of the package. Everything downstream uses it.</param>
/// <param name="Contract">The bytes of <c language="csharp">contract.play</c>.</param>
/// <param name="Parts">The whole artifact.</param>
/// <param name="Signature">The signature evidence, or <see langword="null"/> when the package is unsigned.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Package content is immutable bytes handed as-is to hashing, signature and file APIs.")]
public record RegistryPackage(ExtensionPackageKind Kind, string Resolved, string Version, string Digest, byte[] Contract, IReadOnlyList<ArtifactPart> Parts, SignatureEvidence? Signature);
