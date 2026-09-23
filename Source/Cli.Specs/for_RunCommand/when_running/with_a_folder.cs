// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_folder : given.a_run_command
{
    int _result;

    void Establish() => File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(_folder, "features")).FullName, "invoicing.play"), "domain Invoicing\n");

    async Task Because() => _result = await Run(_folder, "--tag", "1.2.0", "--port", "9191", "--workbench-port", "35001");

    [Unix.Fact] void should_start_docker_once_with_only_the_folder_mounted_read_only() => DockerArguments.Where((_, index) => index != 3).SequenceEqual(["run", "--rm", "--name", "-p", "9191:9090", "-p", "35001:35000", "-v", $"{_folder}:/eventmodel:ro", "cratis/stage:1.2.0"]).ShouldBeTrue();
    [Unix.Fact] void should_report_that_the_container_stopped_before_it_was_ready() => _result.ShouldEqual(ExitCodes.ServerError);
}
