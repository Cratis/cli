// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_missing_package_set : given.a_run_command
{
    int _result;

    void Establish() => File.WriteAllText(Path.Combine(_folder, "invoicing.play"), "domain Invoicing\n");

    async Task Because() => _result = await Run(_folder, "--package-set", Path.Combine(_folder, "missing.json"));

    [Fact] void should_report_the_set_as_not_found() => _result.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_not_start_docker() => DockerArguments.ShouldBeEmpty();
}
