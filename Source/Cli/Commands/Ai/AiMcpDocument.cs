// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

internal sealed class AiMcpDocument : IAiMcpDocument
{
    static readonly JsonDocumentOptions _options = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
    readonly AiMcpFile _file;
    readonly JsonObject _document;
    string _content;
    bool _changed;

    internal AiMcpDocument(string project, string relative)
    {
        _file = new(project, relative);
        _content = _file.Original ?? "{}\n";
        _document = Parse(_content);
    }

    public JsonNode? Get(string collection, string id)
    {
        if (!_document.TryGetPropertyValue(collection, out var servers)) return null;
        if (servers is not JsonObject members) throw new AiMcpConfigurationInvalid($"MCP configuration '{collection}' must be an object.");
        return members[id];
    }

    public bool Contains(string collection, string id) => _document[collection] is JsonObject servers && servers.ContainsKey(id);

    public void Set(string collection, string id, JsonNode? value)
    {
        _changed = true;
        _content = AiJsonMemberEditor.Set(_content, collection, id, value);
        if (_document[collection] is not JsonObject servers)
        {
            servers = [];
            _document[collection] = servers;
        }
        if (value is null) servers.Remove(id);
        else servers[id] = value.DeepClone();
    }

    public void Apply(AiFileOperations operations)
    {
        if (_changed) _file.Apply(_content, operations);
    }

    static JsonObject Parse(string content)
    {
        if (content.StartsWith('\uFEFF')) content = content[1..];
        using var parsed = JsonDocument.Parse(content, _options);
        RejectDuplicateProperties(parsed.RootElement);
        return JsonNode.Parse(content, documentOptions: _options) as JsonObject ?? throw new AiMcpConfigurationInvalid("MCP configuration must be a JSON object.");
    }

    static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new AiMcpConfigurationInvalid($"Duplicate JSON property: {property.Name}");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
        }
    }
}
