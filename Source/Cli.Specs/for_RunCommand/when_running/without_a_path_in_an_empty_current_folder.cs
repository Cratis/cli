// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class without_a_path_in_an_empty_current_folder : given.a_run_command
{
    int _result;

    void Establish() => _settings.Path = null;

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_empty_folder_before_attempting_docker() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_use_the_current_folder() => _error.ToString().ShouldContain("No Screenplay files (.play) found in the folder");
}
