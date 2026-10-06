// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Edits JSONC member token spans, retaining every unrelated byte, including comments and whitespace.
/// </summary>
internal static class AiJsonMemberEditor
{
    static readonly JsonReaderOptions _readerOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    internal static string Set(string content, string collection, string id, JsonNode? value)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var formatting = new AiJsonFormatting(bytes);
        var root = Object(bytes, content.StartsWith('\uFEFF') ? 3 : 0, 0);
        var member = root.Members.FirstOrDefault(member => member.Name == collection);
        if (member is null)
        {
            if (value is null) return content;
            var members = new JsonObject { [id] = value.DeepClone() };
            return Insert(bytes, root, collection, members, formatting);
        }
        var servers = Object(bytes, member.ValueStart, 1);
        var server = servers.Members.FirstOrDefault(member => member.Name == id);
        if (server is null) return value is null ? content : Insert(bytes, servers, id, value, formatting);
        if (value is not null) return Replace(bytes, server.ValueStart, server.ValueEnd, formatting.Serialize(value, formatting.MemberIndentation(server.Start, 2)));
        var after = Comma(bytes, server.ValueEnd, servers.End - 1);
        var removal = Removal(bytes, server, after, formatting);
        var edits = new List<SpanEdit> { removal };
        if (after >= 0)
        {
            if (after < removal.Start || after >= removal.End) edits.Add(new(after, after + 1, string.Empty));
        }
        else
        {
            var index = servers.Members.IndexOf(server);
            if (index > 0)
            {
                var before = Comma(bytes, servers.Members[index - 1].ValueEnd, server.Start);
                if (before >= 0) edits.Add(new(before, before + 1, string.Empty));
            }
        }
        return Apply(bytes, edits);
    }

    static SpanEdit Removal(byte[] bytes, MemberSpan member, int comma, AiJsonFormatting formatting)
    {
        var start = formatting.LineStart(member.Start);
        var end = member.ValueEnd;

        // Only consume complete owned lines. Comments and other members on the same line retain
        // their trivia, and a separator outside this span is removed by a separate edit.
        if (formatting.IsIndentation(start, member.Start))
        {
            while (end < bytes.Length && (bytes[end] is (byte)' ' or (byte)'\t' or (byte)'\r' || end == comma)) end++;
            if (end == bytes.Length || bytes[end] == '\n') return new(start, end < bytes.Length ? end + 1 : end, string.Empty);
        }
        return new(member.Start, member.ValueEnd, string.Empty);
    }

    static string Insert(byte[] bytes, ObjectSpan parent, string name, JsonNode value, AiJsonFormatting formatting)
    {
        var edits = new List<SpanEdit>();
        if (parent.Members.Count > 0)
        {
            var last = parent.Members[^1];
            if (Comma(bytes, last.ValueEnd, parent.End - 1) < 0) edits.Add(new(last.ValueEnd, last.ValueEnd, ","));
        }
        var closing = parent.End - 1;
        var start = formatting.LineStart(closing);
        var ownLine = formatting.IsIndentation(start, closing);
        var closingIndentation = ownLine ? Encoding.UTF8.GetString(bytes, start, closing - start) : formatting.Indentation(parent.Depth);
        var indentation = parent.Members.Count > 0
            ? formatting.MemberIndentation(parent.Members[^1].Start, parent.Depth + 1)
            : closingIndentation + formatting.Indentation(1);
        if (!ownLine)
        {
            start = closing;
            while (start > 0 && bytes[start - 1] is (byte)' ' or (byte)'\t') start--;
        }
        var prefix = ownLine ? string.Empty : formatting.NewLine;
        var property = $"{prefix}{indentation}{JsonSerializer.Serialize(name)}: {formatting.Serialize(value, indentation)}{formatting.NewLine}{closingIndentation}";
        edits.Add(new(start, closing, property));
        return Apply(bytes, edits);
    }

    static ObjectSpan Object(byte[] bytes, int start, int depth)
    {
        var reader = new Utf8JsonReader(bytes.AsSpan(start), _readerOptions);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) throw new AiMcpConfigurationInvalid("MCP server collection must be an object.");
        var members = new List<MemberSpan>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString()!;
            var propertyStart = start + (int)reader.TokenStartIndex;
            reader.Read();
            var valueStart = start + (int)reader.TokenStartIndex;
            reader.Skip();
            members.Add(new(name, propertyStart, valueStart, start + (int)reader.BytesConsumed));
        }
        return new(start + (int)reader.BytesConsumed, depth, members);
    }

    static int Comma(byte[] bytes, int start, int end)
    {
        // Commas are punctuation, not Utf8JsonReader tokens. Inspect only the already-validated trivia
        // between two token spans; a comma in a comment must never be mistaken for a separator.
        for (var index = start; index < end; index++)
        {
            if (bytes[index] == ',') return index;
            if (bytes[index] != '/' || index + 1 >= end) continue;
            if (bytes[index + 1] == '/')
            {
                index += 2;
                while (index < end && bytes[index] != '\n') index++;
            }
            else if (bytes[index + 1] == '*')
            {
                index += 2;
                while (index + 1 < end && !(bytes[index] == '*' && bytes[index + 1] == '/')) index++;
                index++;
            }
        }
        return -1;
    }

    static string Replace(byte[] bytes, int start, int end, string value) => Apply(bytes, [new(start, end, value)]);

    static string Apply(byte[] bytes, List<SpanEdit> edits)
    {
        using var output = new MemoryStream();
        var position = 0;
        foreach (var edit in edits.OrderBy(edit => edit.Start))
        {
            output.Write(bytes, position, edit.Start - position);
            output.Write(Encoding.UTF8.GetBytes(edit.Value));
            position = edit.End;
        }
        output.Write(bytes, position, bytes.Length - position);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    sealed record ObjectSpan(int End, int Depth, List<MemberSpan> Members);
    sealed record MemberSpan(string Name, int Start, int ValueStart, int ValueEnd);
    sealed record SpanEdit(int Start, int End, string Value);
}
