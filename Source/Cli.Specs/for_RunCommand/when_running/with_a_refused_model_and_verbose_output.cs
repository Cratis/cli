// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_refused_model_and_verbose_output : given.a_serving_stage
{
    int _result;

    void Establish() => _statusBody = """{"state":"unsupported","engine":"semantic","issues":[{"capability":"World","artifact":"model","details":"History cannot be reconstructed."}]}""";

    async Task Because()
    {
        if (OperatingSystem.IsWindows()) return;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _result = await new RunCommand().ExecuteAsync(null!, new RunSettings { Path = _folder, Port = _port, Output = OutputFormats.JsonCompact, Verbose = true }, deadline.Token);
    }

    [Unix.Fact] void should_exit_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Unix.Fact] void should_stop_the_container() => DockerArguments.ShouldContain("stop");
    [Unix.Fact] void should_report_the_actual_engine() => _error.ToString().ShouldContain("cannot run on the semantic engine");
}
