// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_a_selected_engine : given.a_run_command
{
    void Establish() => File.WriteAllText(Path.Combine(_folder, "invoicing.play"), "domain Invoicing\n");

    async Task Because() => await Run(_folder, "--engine", "SEMANTIC");

    [Unix.Fact] void should_pass_the_normalized_engine_to_docker() => DockerArguments.ShouldContain("Stage__Runtime__Engine=semantic");
}
