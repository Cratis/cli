// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Writes a generated Screenplay document without altering a single byte of it.
/// </summary>
/// <remarks>
/// The generator produces source that ends with exactly one newline, and round-tripping a <c>.play</c> file has to
/// be byte identical. Everything here therefore writes raw UTF-8 without a byte order mark and never appends,
/// trims, or translates a line ending.
/// </remarks>
public static class ScreenplayDocument
{
    /// <summary>
    /// The base file name used for the document when none is given on the command line.
    /// </summary>
    public const string DefaultFileName = "Screenplay";

    static readonly UTF8Encoding _encoding = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Resolves the full path the document is written to.
    /// </summary>
    /// <param name="file">The file given on the command line.</param>
    /// <param name="currentDirectory">The directory relative paths are resolved against.</param>
    /// <returns>The full path of the file to write.</returns>
    public static string ResolvePath(string file, string currentDirectory) => Path.GetFullPath(file, currentDirectory);

    /// <summary>
    /// Resolves the path to write to when no file was given on the command line.
    /// </summary>
    /// <param name="currentDirectory">The directory the document is written into.</param>
    /// <returns>
    /// <c>Screenplay.play</c> in <paramref name="currentDirectory"/>, or — when that already exists — the first of
    /// <c>Screenplay-1.play</c>, <c>Screenplay-2.play</c>, and so on that does not, so a previous document is never
    /// silently overwritten.
    /// </returns>
    public static string ResolveDefaultPath(string currentDirectory)
    {
        var candidate = Path.Combine(currentDirectory, $"{DefaultFileName}{PlayFileTargetResolver.Extension}");
        for (var index = 1; File.Exists(candidate); index++)
        {
            candidate = Path.Combine(currentDirectory, $"{DefaultFileName}-{index}{PlayFileTargetResolver.Extension}");
        }

        return candidate;
    }

    /// <summary>
    /// Writes the document to a file, creating the folder it lives in when needed.
    /// </summary>
    /// <param name="path">The full path to write to.</param>
    /// <param name="source">The generated <c>.play</c> source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WriteToFile(string path, string source, CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await File.WriteAllBytesAsync(path, _encoding.GetBytes(source), cancellationToken);
    }

    /// <summary>
    /// Writes the document to a stream as raw UTF-8.
    /// </summary>
    /// <param name="stream">The stream to write to. It is left open for the caller to dispose.</param>
    /// <param name="source">The generated <c>.play</c> source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Writing bytes to the raw standard output stream rather than through <see cref="Console.Out"/> is deliberate:
    /// the console writer applies its own encoding and would corrupt anything outside its code page.
    /// </remarks>
    public static async Task Write(Stream stream, string source, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(_encoding.GetBytes(source), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
