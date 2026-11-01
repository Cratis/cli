// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Direct;

/// <summary>The exception that is thrown when a message from Direct exceeds what the bridge will hold in memory.</summary>
/// <param name="limit">The limit in characters.</param>
internal sealed class DirectMcpMessageTooLarge(int limit)
    : Exception($"Direct's MCP endpoint sent a message larger than the bridge accepts ({limit / (1024 * 1024)} MiB).");

/// <summary>Reads text from Direct without letting one message grow beyond a limit.</summary>
/// <param name="reader">The text to read.</param>
/// <param name="limit">The maximum characters of one line, or of the whole text.</param>
internal sealed class DirectMcpBoundedReader(TextReader reader, int limit)
{
    readonly char[] _buffer = new char[8192];
    int _position;
    int _length;
    bool _skipLineFeed;

    /// <summary>Gets the limit in characters.</summary>
    internal int Limit => limit;

    /// <summary>Reads everything that is left.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The text.</returns>
    /// <exception cref="DirectMcpMessageTooLarge">When the text exceeds the limit.</exception>
    internal async Task<string> ReadToEnd(CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        while (await Fill(cancellationToken))
        {
            Append(text, _buffer.AsSpan(_position, _length - _position));
            _position = _length;
        }

        return text.ToString();
    }

    /// <summary>Reads one line ended by a carriage return, a line feed or both.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The line without its end, or null at the end of the text.</returns>
    /// <exception cref="DirectMcpMessageTooLarge">When the line exceeds the limit.</exception>
    internal async Task<string?> ReadLine(CancellationToken cancellationToken)
    {
        var line = new StringBuilder();
        var any = false;
        while (await Fill(cancellationToken))
        {
            if (_skipLineFeed)
            {
                _skipLineFeed = false;
                if (_buffer[_position] == '\n')
                {
                    _position++;
                    continue;
                }
            }

            any = true;
            var available = _buffer.AsSpan(_position, _length - _position);
            var end = available.IndexOfAny('\r', '\n');
            if (end < 0)
            {
                Append(line, available);
                _position = _length;
                continue;
            }

            Append(line, available[..end]);
            _skipLineFeed = available[end] == '\r';
            _position += end + 1;
            return line.ToString();
        }

        return any ? line.ToString() : null;
    }

    void Append(StringBuilder text, ReadOnlySpan<char> characters)
    {
        if (text.Length + characters.Length > limit)
        {
            throw new DirectMcpMessageTooLarge(limit);
        }

        text.Append(characters);
    }

    async Task<bool> Fill(CancellationToken cancellationToken)
    {
        if (_position < _length)
        {
            return true;
        }

        _length = await reader.ReadAsync(_buffer.AsMemory(), cancellationToken);
        _position = 0;
        return _length > 0;
    }
}
