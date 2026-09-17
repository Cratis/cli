// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using System.Text;

namespace Cratis.Templating.FileSystem;

/// <summary>
/// Represents the result of encoding detection for a file: the detected encoding and whether a byte-order
/// mark was present, so that output preserves both.
/// </summary>
/// <param name="Encoding">The detected encoding.</param>
/// <param name="HasBom">Whether a byte-order mark was present.</param>
public record FileEncoding(Encoding Encoding, bool HasBom)
{
    /// <summary>
    /// Gets the UTF-8 fallback encoding used for files without a detectable mark.
    /// </summary>
    public static FileEncoding Utf8NoBom { get; } = new(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), false);

    /// <summary>
    /// Detects the encoding of a byte sequence from its byte-order mark, defaulting to UTF-8 without BOM.
    /// </summary>
    /// <param name="bytes">The file bytes.</param>
    /// <returns>The detected encoding with BOM presence.</returns>
    public static FileEncoding Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return new FileEncoding(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return new FileEncoding(Encoding.Unicode, true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return new FileEncoding(Encoding.BigEndianUnicode, true);
        }

        return Utf8NoBom;
    }
}
