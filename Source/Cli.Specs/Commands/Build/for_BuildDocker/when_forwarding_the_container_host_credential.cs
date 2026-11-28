// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Build.for_BuildDocker;

public class when_forwarding_the_container_host_credential : Specification
{
    ProcessStartInfo _docker;

    void Because() => _docker = BuildProcess.DockerHost(["-e", StudioConnection.BuilderTokenVariable], ".", new Dictionary<string, string> { [BuildConnectSettings.McpSecretVariable] = "private" }, "host-only");

    [Fact] void should_forward_only_the_selected_builder_token() => _docker.Environment[StudioConnection.BuilderTokenVariable].ShouldEqual("host-only");
    [Fact] void should_keep_the_mcp_secret_private() => _docker.Environment.ContainsKey(BuildConnectSettings.McpSecretVariable).ShouldBeFalse();
    [Fact] void should_not_pass_the_value_as_an_argument() => _docker.ArgumentList.Contains("host-only").ShouldBeFalse();
}
