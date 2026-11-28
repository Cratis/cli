// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.Commands.Build.for_DockerCli;

[Collection(CliSpecsCollection.Name)]
public class when_probing_from_a_builder : given.a_builder_process
{
    [Cratis.Cli.for_DirectFileSecrets.given.unix_only.Fact]
    async Task should_isolate_both_probe_modes_from_credentials_and_host_input()
    {
        Script("docker", Check + " && ! read -r line && echo ready");
        Set("PATH", Folder);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        (await DockerCli.Run(["images"], deadline.Token)).Trim().ShouldEqual("ready");
        (await DockerCli.Execute(["images"], deadline.Token)).ExitCode.ShouldEqual(0);
    }
}
