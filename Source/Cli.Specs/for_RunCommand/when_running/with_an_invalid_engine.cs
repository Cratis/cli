// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_invalid_engine : given.a_run_command
{
    int _result;

    void Establish() => File.WriteAllText(Path.Combine(_folder, "invoicing.play"), "domain Invoicing\n");

    async Task Because()
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_error) });
        _result = await CliApp.Create(console).RunAsync(["run", _folder, "--engine", "unknown"]);
    }

    [Fact] void should_reject_the_settings() => _result.ShouldNotEqual(ExitCodes.Success);
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
    [Fact] void should_list_both_engines() => _error.ToString().ShouldContain("eventmodel, semantic");
}
