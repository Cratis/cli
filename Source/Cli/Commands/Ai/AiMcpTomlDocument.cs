// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Adds a native Codex table and removes only its parsed tokens, never rewriting other TOML.
/// </summary>
internal sealed class AiMcpTomlDocument : IAiMcpDocument
{
    readonly AiMcpFile _file;
    string _content;
    bool _changed;

    internal AiMcpTomlDocument(string project, string relative)
    {
        _file = new(project, relative);
        _content = _file.Original ?? string.Empty;
        Parse();
    }

    public JsonNode? Get(string collection, string id)
    {
        var model = TomlSerializer.Deserialize<TomlTable>(WithoutBom(_content))!;
        if (!model.TryGetValue(collection, out var members)) return null;
        if (members is not TomlTable servers) throw new AiMcpConfigurationInvalid($"Codex '{collection}' must be a table.");
        return servers.TryGetValue(id, out var value) ? JsonSerializer.SerializeToNode(value) : null;
    }

    public bool Contains(string collection, string id) => Get(collection, id) is not null;

    public void Set(string collection, string id, JsonNode? value)
    {
        if (Contains(collection, id)) RemoveTable(collection, id);
        if (value is not null)
        {
            TomlArray arguments = [.. value["args"]!.AsArray().Select(item => item!.GetValue<string>())];
            var entry = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["command"] = value["command"]!.GetValue<string>(),
                ["args"] = arguments
            };
            _content += $"\n[{collection}.{id}]\n{TomlSerializer.Serialize(entry)}";
        }

        // Inline parent tables and ambiguous dotted-key shapes must not be extended with invalid TOML.
        Parse();
        _changed = true;
    }

    public void Apply(AiFileOperations operations)
    {
        if (_changed) _file.Apply(_content, operations);
    }

    static IEnumerable<string> Parts(KeySyntax key) => new[] { Value(key.Key!) }.Concat(key.DotKeys.Select(item => Value(item.Key!)));
    static string Value(BareKeyOrStringValueSyntax key) => key is StringValueSyntax quoted ? quoted.Value! : ((BareKeySyntax)key).Key!.Text!;
    static string WithoutBom(string content) => content.StartsWith('\uFEFF') ? content[1..] : content;

    void RemoveTable(string collection, string id)
    {
        var document = Parse();
        var table = document.Tables.SingleOrDefault(table => Parts(table.Name!).SequenceEqual([collection, id], StringComparer.Ordinal))
            ?? throw new AiMcpConfigurationInvalid("Owned Codex MCP entry uses an inline or dotted-key shape that cannot be removed losslessly. Restore the installed table before updating.");
        var offset = _content.StartsWith('\uFEFF') ? 1 : 0;
        var spans = table.Items.Select(item => (Start: item.Key!.Span.Start.Offset, End: item.Value!.Span.End.Offset + 1))
            .Append((Start: table.OpenBracket!.Span.Start.Offset, End: table.CloseBracket!.Span.End.Offset + 1));
        foreach (var span in spans.OrderByDescending(span => span.Start)) _content = _content.Remove(span.Start + offset, span.End - span.Start);
    }

    DocumentSyntax Parse()
    {
        var document = SyntaxParser.Parse(WithoutBom(_content));
        if (document.HasErrors) throw new AiMcpConfigurationInvalid($"Invalid or unsupported Codex TOML: {document.Diagnostics}");
        return document;
    }
}
