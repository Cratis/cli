// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// Lays a fetched package out as the folder it ships as, so a reference and a folder are verified the same way.
/// </summary>
public static class ExtensionPackageExtractor
{
    /// <summary>
    /// Extracts a package into a folder. An OCI artifact's files are the ones under <c language="text">extension/</c> in its layers; an npm package's are the ones under <c language="text">package/</c> in its tarball.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="destination">The folder to extract into.</param>
    public static void Extract(RegistryPackage package, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var part in package.Parts)
        {
            if (package.Kind == ExtensionPackageKind.Oci && part.Name == "contract.play")
            {
                File.WriteAllBytes(Path.Combine(destination, "contract.play"), part.Content);
            }
            else if (package.Kind == ExtensionPackageKind.Oci && part.MediaType.Contains("tar", StringComparison.OrdinalIgnoreCase))
            {
                ExtractTar(part.Content, "extension/", destination);
            }
            else if (package.Kind == ExtensionPackageKind.Npm && part.Name == NpmExtensionRegistry.TarballPart)
            {
                ExtractTar(part.Content, "package/", destination);
            }
        }
    }

    static void ExtractTar(byte[] content, string prefix, string destination)
    {
        using var raw = new MemoryStream(content);
        using var stream = content.Length > 2 && content[0] == 0x1f && content[1] == 0x8b ? new GZipStream(raw, CompressionMode.Decompress) : (Stream)raw;
        using var reader = new TarReader(stream);
        var root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        while (reader.GetNextEntry() is { } entry)
        {
            var name = entry.Name.TrimStart('.', '/');
            if (entry.DataStream is null || !name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var target = Path.GetFullPath(Path.Combine(destination, name[prefix.Length..]));
            if (!target.StartsWith(root, StringComparison.Ordinal)) throw new InvalidDataException($"The package holds '{entry.Name}', which is outside the package.");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var file = File.Create(target);
            entry.DataStream.CopyTo(file);
        }
    }
}
