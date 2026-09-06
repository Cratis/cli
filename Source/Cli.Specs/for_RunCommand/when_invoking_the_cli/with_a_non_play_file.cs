// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_invoking_the_cli;

[Collection(CliSpecsCollection.Name)]
public class with_a_non_play_file : given.a_run_command
{
    int _result;

    void Establish()
    {
        _settings.Path = Path.Combine(_folder, "not a screenplay.txt");
        File.WriteAllText(_settings.Path, "not a screenplay");
    }

    async Task Because()
    {
        // PATH contains only the temporary input folder, with no Docker executable. Reaching Docker would
        // return ConnectionError instead; exercise real registration and argument binding, not a string helper.
        _result = await CliApp.Create().RunAsync(["run", _settings.Path!, "--output", OutputFormats.JsonCompact]);
    }

    [Fact] void should_reject_the_input_before_attempting_docker() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_file_admission_error() => _error.ToString().ShouldContain("is not a Screenplay (.play) file");
}
