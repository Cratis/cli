// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Adds the managed-by-Cratis marker to corpus content in the comment syntax of its file type.</summary>
static class AiCorpusMarker
{
    static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    internal static string Add(string source, string path, string content)
    {
        var marker = $"cratis-ai-managed: {source}";
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".md" or ".mdc" => AddMarkdownMarker(marker, content),
            ".html" or ".htm" => $"<!-- {marker} -->\n{content}",
            ".ts" or ".tsx" or ".js" or ".mjs" or ".cjs" or ".cs" => AddScriptMarker($"// {marker}", content),
            ".css" or ".scss" => $"/* {marker} */\n{content}",
            ".json" => AddJsonMarker(source, content),
            _ => AddScriptMarker($"# {marker}", content)
        };
    }

    static string AddMarkdownMarker(string marker, string content)
    {
        if (!content.StartsWith("---\n", StringComparison.Ordinal)) return $"<!-- {marker} -->\n{content}";
        var frontmatterEnd = content.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        return frontmatterEnd < 0 ? $"<!-- {marker} -->\n{content}" : content.Insert(frontmatterEnd + 5, $"<!-- {marker} -->\n");
    }

    static string AddJsonMarker(string source, string content)
    {
        if (JsonNode.Parse(content) is not JsonObject json) return content;
        json["$cratisAiManaged"] = source;
        return json.ToJsonString(_serializerOptions);
    }

    static string AddScriptMarker(string marker, string content)
    {
        if (!content.StartsWith("#!", StringComparison.Ordinal)) return $"{marker}\n{content}";
        var lineEnd = content.IndexOf('\n');
        if (lineEnd < 0) return $"{content}\n{marker}\n";
        var newline = lineEnd > 0 && content[lineEnd - 1] == '\r' ? "\r\n" : "\n";

        return content.Insert(lineEnd + 1, $"{marker}{newline}");
    }
}
