// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_colon_in_the_file_name : given.a_run_command
{
    int _result;

    void Establish()
    {
        _settings.Path = Path.Combine(_folder, "model:part.play");
        if (!OperatingSystem.IsWindows())
        {
            File.WriteAllText(_settings.Path, "domain Selected\n");
        }
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_path_before_attempting_docker() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_explain_the_bounded_mount_syntax() => _error.ToString().ShouldContain("cannot represent");
}
