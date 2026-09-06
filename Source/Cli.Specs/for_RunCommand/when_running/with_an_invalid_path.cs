// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_invalid_path : given.a_run_command
{
    int _result;

    void Establish() => _settings.Path = "invalid\0.play";

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_invalid_path_before_attempting_docker() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_invalid_path() => _error.ToString().ShouldContain("path is invalid");
}
