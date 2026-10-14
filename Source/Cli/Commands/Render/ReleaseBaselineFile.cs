// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Reads bounded release metadata from one admitted regular-file handle.
/// </summary>
internal static class ReleaseBaselineFile
{
    internal static ReleaseBaselineRead Read(string path, ApplicationIdentity application, bool required = false) =>
        Read(path, bytes => ReleaseBaselineStore.ReadBytes(bytes, application), required);

    static ReleaseBaselineRead Read(string path, Func<byte[]?, ReleaseBaselineRead> parse, bool required)
    {
        try
        {
            using var handle = WorkspaceFile.Open(path, CancellationToken.None);
            using var stream = new FileStream(handle, FileAccess.Read, 65536, isAsync: OperatingSystem.IsWindows());
            using var bytes = new MemoryStream();
            var buffer = new byte[65536];
            while (true)
            {
                var remaining = ReleaseBaselineStore.MaximumBytes - checked((int)bytes.Length);
                var read = stream.Read(buffer, 0, Math.Min(buffer.Length, remaining + 1));
                if (read > remaining)
                {
                    return Unreadable("Release baseline exceeds the bounded file size.");
                }

                if (read == 0)
                {
                    break;
                }

                bytes.Write(buffer, 0, read);
            }

            return parse(bytes.ToArray());
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return required ? Unreadable($"the release baseline file '{path}' does not exist") : parse(null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or JsonException or InvalidSemanticContract or InvalidOperationException or KeyNotFoundException)
        {
            return Unreadable(exception.Message);
        }
    }

    static ReleaseBaselineRead Unreadable(string reason) => new("unreadable", null, null, reason);
}
