// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// Recognizes the image formats an icon or marketplace image may have by what is in the file, not by its name.
/// </summary>
public static class ExtensionImageFormat
{
    /// <summary>The formats accepted, as the file extensions they are written with.</summary>
    public static readonly IReadOnlyList<string> Supported = ["png", "jpg", "webp", "svg"];

    /// <summary>
    /// Finds the format of an image's content.
    /// </summary>
    /// <param name="content">The file's bytes.</param>
    /// <returns>One of <see cref="Supported"/>, or <see langword="null"/> when the content is none of them.</returns>
    public static string? Of(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 8 && content[..8].SequenceEqual((byte[])[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return "png";
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF) return "jpg";
        if (content.Length >= 12 && content[..4].SequenceEqual("RIFF"u8) && content[8..12].SequenceEqual("WEBP"u8)) return "webp";
        var head = Encoding.UTF8.GetString(content[..Math.Min(content.Length, 1024)]);
        return head.Contains("<svg", StringComparison.OrdinalIgnoreCase) ? "svg" : null;
    }

    /// <summary>
    /// Gets the format a file name claims.
    /// </summary>
    /// <param name="path">The file name or path.</param>
    /// <returns>The format, with <c language="text">jpeg</c> as <c language="text">jpg</c>.</returns>
    public static string Claimed(string path)
    {
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return extension == "jpeg" ? "jpg" : extension;
    }
}
