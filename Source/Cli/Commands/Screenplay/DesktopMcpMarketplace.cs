// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Preserves unrelated marketplace properties and the raw content of every foreign plugin.</summary>
internal sealed class DesktopMcpMarketplace
{
    internal const string PluginName = "cratis-screenplay";
    readonly string _content;
    readonly List<string> _entries;
    readonly int _start;
    readonly int _end;
    readonly bool _missing;
    readonly bool _hasProperties;

    internal DesktopMcpMarketplace(string content)
    {
        _content = content;
        using var document = JsonDocument.Parse(content.TrimStart('\uFEFF'));
        Validate(document.RootElement);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new AiMcpConfigurationInvalid("Personal marketplace must be a JSON object.");
        _hasProperties = document.RootElement.EnumerateObject().Any();
        _entries = [];
        var bytes = Encoding.UTF8.GetBytes(content);
        var offset = content.StartsWith('\uFEFF') ? 3 : 0;
        var reader = new Utf8JsonReader(bytes.AsSpan(offset));
        reader.Read();
        var found = false;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString();
            reader.Read();
            if (name == "plugins")
            {
                if (reader.TokenType != JsonTokenType.StartArray) throw new AiMcpConfigurationInvalid("Personal marketplace plugins must be an array.");
                _start = offset + (int)reader.TokenStartIndex;
                reader.Skip();
                _end = offset + (int)reader.BytesConsumed;
                found = true;
            }
            else
            {
                reader.Skip();
            }
        }
        _missing = !found;
        if (_missing) _start = _end = offset + (int)reader.TokenStartIndex;
        if (document.RootElement.TryGetProperty("plugins", out var plugins))
        {
            foreach (var plugin in plugins.EnumerateArray())
            {
                if (plugin.ValueKind != JsonValueKind.Object || !plugin.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
                    throw new AiMcpConfigurationInvalid("Every marketplace plugin must have a string name.");
                _entries.Add(plugin.GetRawText());
            }
        }
        if (_entries.Count(IsScreenplay) > 1) throw new AiMcpConfigurationInvalid("Duplicate Screenplay marketplace entries; resolve them before continuing.");
    }

    internal JsonNode? Entry => _entries.Where(IsScreenplay).Select(value => JsonNode.Parse(value)).SingleOrDefault();

    internal string Set(JsonNode? expected, JsonNode? replacement)
    {
        if (!JsonNode.DeepEquals(Entry, expected)) throw new AiMcpConfigurationInvalid("Screenplay marketplace entry is foreign or has been modified. It will not be overwritten.");
        if (replacement is null && Entry is null) return _content;
        var entries = _entries.Where(value => !IsScreenplay(value)).ToList();
        if (replacement is not null) entries.Add(replacement.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var array = "[\n" + string.Join(",\n", entries) + "\n]";
        if (_missing) array = (_hasProperties ? "," : string.Empty) + "\n\"plugins\": " + array + "\n";
        var bytes = Encoding.UTF8.GetBytes(_content);
        return Encoding.UTF8.GetString(bytes.AsSpan(0, _start)) + array + Encoding.UTF8.GetString(bytes.AsSpan(_end));
    }

    static bool IsScreenplay(string value) => JsonNode.Parse(value)!["name"]!.GetValue<string>() == PluginName;

    static void Validate(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new AiMcpConfigurationInvalid($"Duplicate marketplace property: {property.Name}");
                Validate(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in element.EnumerateArray()) Validate(value);
        }
    }
}
