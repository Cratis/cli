// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Writes at explicit offsets on the verified handle and records possible mutation before native I/O.
/// The handle belongs to the caller, not this stream.
/// </summary>
/// <param name="handle">The verified configuration handle.</param>
/// <param name="writing">Records a possible mutation, including a native write that fails partway.</param>
internal sealed class AiConfigurationWriteStream(SafeFileHandle handle, Action writing) : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => RandomAccess.GetLength(handle);
    public override long Position { get; set; }

    public override void Flush() => RandomAccess.FlushToDisk(handle);

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.IsEmpty) return;
        writing();
        RandomAccess.Write(handle, buffer, Position);
        Position += buffer.Length;
    }

    public override void SetLength(long value)
    {
        if (Length == value) return;
        writing();
        RandomAccess.SetLength(handle, value);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new IOException("Unsupported configuration stream seek origin.")
        };
        return Position;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new IOException("Configuration write stream does not support reading.");
}
