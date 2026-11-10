// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Reads a kit manifest, written as YAML or JSON, into the JSON document the manifest schema describes. A plain YAML
/// scalar <see langword="true"/> or <see langword="false"/> is a boolean and every other scalar a string, so a manifest reads the same whichever
/// of the two it is written in.
/// </summary>
public static class BuildKitManifestText
{
    /// <summary>The file name of a manifest written in YAML.</summary>
    public const string YamlFileName = "build-kit.yaml";

    /// <summary>The file name of a manifest written in JSON.</summary>
    public const string JsonFileName = "build-kit.json";

    /// <summary>
    /// Finds the manifest at the root of a kit.
    /// </summary>
    /// <param name="kitRoot">The kit's root folder.</param>
    /// <returns>The manifest's path, or <see langword="null"/> when the kit has none.</returns>
    public static string? Find(string kitRoot) =>
        new[] { YamlFileName, JsonFileName }.Select(name => Path.Combine(kitRoot, name)).FirstOrDefault(File.Exists);

    /// <summary>
    /// Reads a manifest's text.
    /// </summary>
    /// <param name="text">The manifest's text.</param>
    /// <param name="isJson">Whether the text is JSON rather than YAML.</param>
    /// <returns>The manifest as JSON.</returns>
    /// <exception cref="InvalidBuildKit">The text cannot be read.</exception>
    public static JsonObject Read(string text, bool isJson)
    {
        try
        {
            if (isJson)
            {
                return JsonNode.Parse(text) as JsonObject ?? throw new InvalidBuildKit(["(document): the manifest is not an object."]);
            }

            var stream = new YamlStream();
            using var reader = new StringReader(text);
            stream.Load(reader);
            if (stream.Documents.Count == 0 || ToJson(stream.Documents[0].RootNode) is not JsonObject manifest)
            {
                throw new InvalidBuildKit(["(document): the manifest is not a mapping."]);
            }

            return manifest;
        }
        catch (JsonException exception)
        {
            throw new InvalidBuildKit([$"(document): the manifest is not JSON: {exception.Message}"]);
        }
        catch (YamlException exception)
        {
            throw new InvalidBuildKit([$"(document): the manifest is not YAML: {exception.Message}"]);
        }
    }

    static JsonNode? ToJson(YamlNode node) => node switch
    {
        YamlMappingNode mapping => ToObject(mapping),
        YamlSequenceNode sequence => new JsonArray([.. sequence.Children.Select(ToJson)]),
        YamlScalarNode { Style: ScalarStyle.Plain, Value: "true" } => JsonValue.Create(true),
        YamlScalarNode { Style: ScalarStyle.Plain, Value: "false" } => JsonValue.Create(false),
        YamlScalarNode { Style: ScalarStyle.Plain, Value: null or "" or "~" or "null" } => null,
        YamlScalarNode scalar => JsonValue.Create(scalar.Value),
        _ => null
    };

    static JsonObject ToObject(YamlMappingNode mapping)
    {
        var result = new JsonObject();
        foreach (var (key, value) in mapping.Children)
        {
            var name = (key as YamlScalarNode)?.Value ?? string.Empty;
            if (result.ContainsKey(name)) throw new InvalidBuildKit([$"/{name}: the member is written twice."]);
            result[name] = ToJson(value);
        }

        return result;
    }
}
