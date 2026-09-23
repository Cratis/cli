// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Bounds workspace transport bytes before invoking the shared canonical reader.
/// </summary>
internal static class RenderWorkspaceInput
{
    internal const int MaximumBytes = 32 * 1024 * 1024;

    internal static async Task<ScreenplayWorkspace> Read(string path, CancellationToken cancellationToken)
    {
        using var handle = WorkspaceFile.Open(path, cancellationToken);

        // The Unix descriptor is synchronous; ReadAsync schedules regular-file I/O on the pool.
        // No pathname reopen or seekability heuristic may replace the opened-handle type check.
        await using var stream = new FileStream(handle, FileAccess.Read, 65536, isAsync: OperatingSystem.IsWindows());

        return await Read(stream, cancellationToken);
    }

    /// <summary>
    /// Reads bounded bytes without trusting stream length, including short reads and concurrent growth.
    /// </summary>
    /// <param name="stream">The input stream, owned by the caller.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated canonical workspace.</returns>
    /// <exception cref="InvalidScreenplayWorkspace">The envelope exceeds the input limit or fails canonical validation.</exception>
    internal static async Task<ScreenplayWorkspace> Read(Stream stream, CancellationToken cancellationToken)
    {
        await using var bytes = new MemoryStream();
        var buffer = new byte[65536];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = MaximumBytes - checked((int)bytes.Length);
            var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining + 1)), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (read > remaining)
            {
                throw new InvalidScreenplayWorkspace("Workspace input exceeds the 32 MiB envelope limit.");
            }

            if (read == 0)
            {
                break;
            }

            await bytes.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var workspace = ScreenplayWorkspaceSerializer.Deserialize(bytes.GetBuffer().AsSpan(0, checked((int)bytes.Length)));
        cancellationToken.ThrowIfCancellationRequested();
        return workspace;
    }
}
