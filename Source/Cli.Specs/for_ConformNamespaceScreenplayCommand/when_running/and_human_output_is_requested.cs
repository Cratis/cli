// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class and_human_output_is_requested : given.a_conform_namespace_command
{
    string[] _statements = [];
    string _human = string.Empty;

    void Establish()
    {
        Appended(1, _register);
        Appended(1, type: "Undeclared");
    }

    async Task Because()
    {
        await Run();
        _statements = [.. Json.GetProperty("findings").EnumerateArray().Select(finding => finding.GetProperty("statement").GetString()!)
            .Concat(Json.GetProperty("notEstablished").EnumerateArray().Select(statement => statement.GetString()!))];
        _settings.Output = OutputFormats.Plain;
        await Run();
        _human = _output;
    }

    [Fact] void should_have_findings_to_compare() => _statements.ShouldNotBeEmpty();
    [Fact] void should_print_every_json_statement() => _statements.Where(statement => !_human.Contains(statement, StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_print_the_verdict() => _human.ShouldContain("Observation comparison: drift");
}
