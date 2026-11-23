// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Cli.Commands.Chronicle.CheckRelease;
using Cratis.Cli.given;

namespace Cratis.Cli.for_CheckReleaseCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class the_server_cannot_be_reached : a_temp_config_directory
{
    int _result;
    string _output = string.Empty;
    string _file = string.Empty;

    void Establish()
    {
        _file = Path.Combine(_tempConfigHome, "release.json");
        File.WriteAllText(_file, given.a_release_file.Valid);
    }

    async Task Because()
    {
        var writer = new StringWriter();
        var previous = Console.Out;
        try
        {
            Console.SetOut(writer);
            _result = await ((ICommand<CheckReleaseSettings>)new CheckReleaseCommand()).ExecuteAsync(
                new CommandContext([], Substitute.For<IRemainingArguments>(), "check-release", null),
                new CheckReleaseSettings { Server = "chronicle://127.0.0.1:1/?disableTls=true", Release = _file, Output = OutputFormats.JsonCompact },
                CancellationToken.None);
        }
        finally
        {
            Console.SetOut(previous);
        }

        _output = writer.ToString();
    }

    [Fact] void should_exit_with_connection_error() => _result.ShouldEqual(ExitCodes.ConnectionError);
    [Fact] void should_report_the_shared_format_on_stdout() => _output.ShouldContain("\"schema\":\"cratis.compat/v1\"");
    [Fact] void should_report_an_unknown_verdict() => _output.ShouldContain("\"verdict\":\"unknown\"");
}
