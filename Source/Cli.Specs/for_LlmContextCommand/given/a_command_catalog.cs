// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_LlmContextCommand.given;

public class a_command_catalog : Specification
{
    protected static readonly string[] _effects = ["read-only", "local", "mutating", "destructive"];

    protected static JsonObject CommandAt(string catalog, string path) =>
        AllCommands(catalog)[path];

    protected static IDictionary<string, JsonObject> AllCommands(string catalog) =>
        JsonNode.Parse(catalog)!["commandGroups"]!.AsArray()
            .SelectMany(group => CommandsIn(group!.AsObject(), []))
            .ToDictionary(command => command.Key, command => command.Value, StringComparer.Ordinal);

    static IEnumerable<KeyValuePair<string, JsonObject>> CommandsIn(JsonObject group, string[] parents)
    {
        var name = group["name"]!.GetValue<string>();
        var path = name == "(root)" ? parents : [.. parents, name];
        var commands = (group["commands"]?.AsArray() ?? [])
            .Select(command => KeyValuePair.Create(string.Join(' ', [.. path, command!["name"]!.GetValue<string>()]), command!.AsObject()));
        var nested = (group["subGroups"]?.AsArray() ?? [])
            .SelectMany(subGroup => CommandsIn(subGroup!.AsObject(), path));
        return commands.Concat(nested);
    }
}
