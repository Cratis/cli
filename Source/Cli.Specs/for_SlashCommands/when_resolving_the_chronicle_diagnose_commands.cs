// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Init;
using Cratis.Cli.Commands.LlmContext;

namespace Cratis.Cli.for_SlashCommands;

public class when_resolving_the_chronicle_diagnose_commands : Specification
{
    HashSet<string> _known;
    string[] _referenced;

    void Establish() => _known = [.. CommandsIn(JsonNode.Parse(LlmContextCommand.BuildDescriptorJson())!["commandGroups"]!.AsArray(), [])];

    void Because() => _referenced = [.. SlashCommands.ChronicleDiagnose.Split('`')
        .Where((_, index) => index % 2 == 1)
        .Where(code => code.StartsWith("cratis ", StringComparison.Ordinal))
        .Select(code => string.Join(' ', code.Split(' ').Skip(1).TakeWhile(word => !word.StartsWith('-') && !word.StartsWith('<'))))];

    [Fact] void should_reference_commands() => _referenced.ShouldNotBeEmpty();
    [Fact] void should_reference_only_commands_the_cli_has() => _referenced.Where(command => !_known.Contains(command)).ShouldBeEmpty();
    [Fact] void should_list_observers_through_the_chronicle_branch() => _referenced.ShouldContain("chronicle observers list");

    static IEnumerable<string> CommandsIn(JsonArray groups, string[] parents) =>
        groups.Select(group => group!.AsObject()).SelectMany(group =>
        {
            var name = group["name"]!.GetValue<string>();
            var path = name == "(root)" ? parents : [.. parents, name];
            var commands = (group["commands"]?.AsArray() ?? []).Select(command => string.Join(' ', [.. path, command!["name"]!.GetValue<string>()]));
            return commands.Concat(CommandsIn(group["subGroups"]?.AsArray() ?? [], path));
        });
}
