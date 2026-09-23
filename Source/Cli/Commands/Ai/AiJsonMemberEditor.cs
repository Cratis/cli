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
    static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    static readonly JsonReaderOptions _readerOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    internal static string Set(string content, string collection, string id, JsonNode? value)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var root = Object(bytes, content.StartsWith('\uFEFF') ? 3 : 0);
        var member = root.Members.FirstOrDefault(member => member.Name == collection);
        if (member is null)
        {
            if (value is null) return content;
            var members = new JsonObject { [id] = value.DeepClone() };
            return Insert(bytes, root, collection, members);
        }
        var servers = Object(bytes, member.ValueStart);
        var server = servers.Members.FirstOrDefault(member => member.Name == id);
        if (server is null) return value is null ? content : Insert(bytes, servers, id, value);
        if (value is not null) return Replace(bytes, server.ValueStart, server.ValueEnd, value.ToJsonString(_options));
        var after = Comma(bytes, server.ValueEnd, servers.End - 1);
        var edits = new List<SpanEdit> { new(server.Start, server.ValueEnd, string.Empty) };
        if (after >= 0)
        {
            edits.Add(new(after, after + 1, string.Empty));
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

    static string Insert(byte[] bytes, ObjectSpan parent, string name, JsonNode value)
    {
        var edits = new List<SpanEdit>();
        if (parent.Members.Count > 0)
        {
            var last = parent.Members[^1];
            if (Comma(bytes, last.ValueEnd, parent.End - 1) < 0) edits.Add(new(last.ValueEnd, last.ValueEnd, ","));
        }
        var property = $"\n  {JsonSerializer.Serialize(name)}: {value.ToJsonString(_options)}\n";
        edits.Add(new(parent.End - 1, parent.End - 1, property));
        return Apply(bytes, edits);
    }

    static ObjectSpan Object(byte[] bytes, int start)
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
        return new(start + (int)reader.BytesConsumed, members);
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

    sealed record ObjectSpan(int End, List<MemberSpan> Members);
    sealed record MemberSpan(string Name, int Start, int ValueStart, int ValueEnd);
    sealed record SpanEdit(int Start, int End, string Value);
}
