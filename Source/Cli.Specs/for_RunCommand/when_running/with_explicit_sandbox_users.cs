// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_explicit_sandbox_users : given.a_run_command
{
    void Establish() => File.WriteAllText(Path.Combine(_folder, "app.play"), "user guest \"Guest\"\ndomain Invoicing\n");

    async Task Because() => await Run(_folder, "--stage-setting", "Sandbox:Users=true", "--stage-setting", "Sandbox:UsersAllowRemote=true");

    [Unix.Fact] void should_enable_modeled_users() => DockerArguments.ShouldContain("Stage__Sandbox__Users=true");
    [Unix.Fact] void should_allow_host_traffic_into_the_container() => DockerArguments.ShouldContain("Stage__Sandbox__UsersAllowRemote=true");
    [Unix.Fact] void should_keep_the_host_binding_local() => DockerArguments.ShouldContain("127.0.0.1:9090:9090");
}
