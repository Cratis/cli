// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Threading.Channels;

namespace Cratis.Cli.Commands.Build;

/// <summary>Watches an append-only UTF-8 step file without polling or replaying earlier bytes.</summary>
public sealed class GeneratorProgressFile : IAsyncDisposable
{
    readonly FileStream _file;
    readonly FileSystemWatcher? _watcher;
    readonly Action<BuildProgress> _progress;
    readonly Action<string> _log;
    readonly Channel<Change> _changes;
    readonly Decoder _decoder = new UTF8Encoding(false, false).GetDecoder();
    readonly StringBuilder _line = new();
    readonly Task _reading;
    readonly string _path;
    int _invalid;
    bool _atStart;
    bool _finished;
    bool _truncated;

    /// <summary>Starts watching from an already-consumed offset.</summary>
    /// <param name="path">The host-created file.</param>
    /// <param name="offset">Bytes consumed by previous prompts.</param>
    /// <param name="progress">Receives steps.</param>
    /// <param name="log">Receives local diagnostics.</param>
    public GeneratorProgressFile(string path, long offset, Action<BuildProgress> progress, Action<string> log)
        : this(path, offset, progress, log, Channel.CreateBounded<Change>(1))
    {
        try
        {
            _watcher = new(Path.GetDirectoryName(path)!, Path.GetFileName(path)) { NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.FileName };
            _watcher.Changed += (_, _) => _changes.Writer.TryWrite(new(ChangeKind.Changed));
            _watcher.Deleted += (_, _) => Invalidate();
            _watcher.Renamed += (_, _) => Invalidate();
            _watcher.Created += (_, _) => _changes.Writer.TryWrite(new(ChangeKind.Changed));
            _watcher.Error += (_, _) => _changes.Writer.TryWrite(new(ChangeKind.Error));
            _watcher.EnableRaisingEvents = true;
            _changes.Writer.TryWrite(new(ChangeKind.Changed));
        }
        catch
        {
            _watcher?.Dispose();
            _changes.Writer.TryComplete();
            _file.Dispose();
            throw;
        }
    }

    internal GeneratorProgressFile(string path, long offset, Action<BuildProgress> progress, Action<string> log, Channel<Change> changes)
    {
        _path = path;
        _progress = progress;
        _log = log;
        _changes = changes;
        _atStart = offset == 0;
        _file = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous) { Position = offset };
        _reading = ReadChanges();
    }

    internal enum ChangeKind
    {
        Changed,
        Error
    }

    /// <summary>Gets the consumed byte offset for the next prompt.</summary>
    public long Offset { get; private set; }

    /// <summary>Stops notifications and drains any final, unterminated step.</summary>
    /// <returns>The drain task.</returns>
    public async Task Complete()
    {
        if (_finished) return;
        _finished = true;
        _watcher?.Dispose();
        _changes.Writer.TryComplete();
        await _reading;
        await Read();
        if (_invalid == 0)
        {
            var final = new char[4];
            var count = _decoder.GetChars([], 0, 0, final, 0, true);
            for (var index = 0; index < count; index++) Append(final[index]);
            Emit();
        }
        Offset = _file.Position;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await Complete();
        await _file.DisposeAsync();
    }

    async Task ReadChanges()
    {
        while (await _changes.Reader.WaitToReadAsync())
        {
            while (_changes.Reader.TryRead(out var change))
            {
                try
                {
                    await Read();
                    change.Processed?.TrySetResult();
                }
                catch (Exception exception)
                {
                    change.Processed?.TrySetException(exception);
                    throw;
                }
            }
        }
    }

    async Task Read()
    {
        if (_invalid != 0) return;
        try
        {
            if (WasReplaced() || _file.Length < _file.Position)
            {
                Invalidate();
                return;
            }

            var bytes = new byte[4096];
            var chars = new char[8192];
            var remaining = _file.Length - _file.Position;
            int count;
            while (remaining > 0 && (count = await _file.ReadAsync(bytes.AsMemory(0, (int)Math.Min(bytes.Length, remaining)))) > 0)
            {
                remaining -= count;
                var length = _decoder.GetChars(bytes, 0, count, chars, 0, false);
                for (var index = 0; index < length; index++) Append(chars[index]);
            }
        }
        catch (IOException)
        {
            _log("Generator progress file could not be read; waiting for another change.");
        }
    }

    void Append(char character)
    {
        if (_atStart)
        {
            _atStart = false;
            if (character == '\uFEFF') return;
        }

        if (character == '\n') Emit();
        else if (_line.Length < 65536) _line.Append(character);
        else _truncated = true;
    }

    void Emit()
    {
        if (_truncated) _log("Generator progress step exceeded the read limit; dropped it.");
        var step = _line.ToString().Trim();
        if (!_truncated && step.Length > 0) _progress(new(step));
        _line.Clear();
        _truncated = false;
    }

    bool WasReplaced() => !File.Exists(_path) || File.GetCreationTimeUtc(_path) != File.GetCreationTimeUtc(_file.SafeFileHandle);

    void Invalidate()
    {
        if (Interlocked.Exchange(ref _invalid, 1) != 0) return;
        _log("Generator progress file was replaced, truncated or could not be watched; ignoring further progress.");
    }

    internal sealed record Change(ChangeKind Kind, TaskCompletionSource? Processed = null);
}
