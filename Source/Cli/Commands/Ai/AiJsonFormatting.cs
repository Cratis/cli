// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Formats only new JSON tokens using the surrounding document's indentation and line endings.
/// </summary>
internal sealed class AiJsonFormatting
{
    readonly byte[] _bytes;
    readonly string _unit;
    readonly JsonSerializerOptions _options;

    internal AiJsonFormatting(byte[] bytes)
    {
        _bytes = bytes;
        var newline = Array.IndexOf(bytes, (byte)'\n');
        NewLine = newline > 0 && bytes[newline - 1] == '\r' ? "\r\n" : "\n";
        _unit = IndentationUnit();
        _options = new() { WriteIndented = true, IndentCharacter = _unit[0], IndentSize = _unit.Length, NewLine = NewLine };
    }

    internal string NewLine { get; }

    internal string Indentation(int depth) => string.Concat(Enumerable.Repeat(_unit, depth));

    internal string Serialize(JsonNode value, string indentation) => value.ToJsonString(_options).Replace(NewLine, NewLine + indentation, StringComparison.Ordinal);

    internal int LineStart(int position)
    {
        while (position > 0 && _bytes[position - 1] != '\n') position--;
        return position;
    }

    internal bool IsIndentation(int start, int end)
    {
        for (var index = start; index < end; index++)
        {
            if (_bytes[index] is not (byte)' ' and not (byte)'\t') return false;
        }
        return true;
    }

    internal string MemberIndentation(int position, int depth)
    {
        var start = LineStart(position);
        return IsIndentation(start, position) ? Encoding.UTF8.GetString(_bytes, start, position - start) : Indentation(depth);
    }

    string IndentationUnit()
    {
        var offset = _bytes.Length >= 3 && _bytes[0] == 0xEF && _bytes[1] == 0xBB && _bytes[2] == 0xBF ? 3 : 0;
        var reader = new Utf8JsonReader(_bytes.AsSpan(offset), new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            var position = offset + (int)reader.TokenStartIndex;
            var start = LineStart(position);
            var length = position - start;
            if (length == 0 || length % reader.CurrentDepth != 0 || !IsIndentation(start, position)) continue;
            var unit = Encoding.UTF8.GetString(_bytes, start, length / reader.CurrentDepth);
            if (unit.All(character => character == unit[0])) return unit;
        }
        return "  ";
    }
}
