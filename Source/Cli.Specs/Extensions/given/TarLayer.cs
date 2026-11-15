// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Builds a tar layer, as an OCI artifact or an npm tarball holds its files.
/// </summary>
static class TarLayer
{
    /// <summary>
    /// Builds a tar, gzipped when asked, from entries.
    /// </summary>
    /// <param name="gzip">Whether to compress it.</param>
    /// <param name="entries">The entries by their name in the tar.</param>
    /// <returns>The bytes.</returns>
    public static byte[] Build(bool gzip, IReadOnlyDictionary<string, string> entries)
    {
        using var output = new MemoryStream();
        using (var target = gzip ? new GZipStream(output, CompressionLevel.Fastest, true) : (Stream)output)
        using (var writer = new TarWriter(target, true))
        {
            foreach (var (name, content) in entries)
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)) });
            }
        }

        return output.ToArray();
    }
}
