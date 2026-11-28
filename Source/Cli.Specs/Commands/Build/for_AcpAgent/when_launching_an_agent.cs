// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_AcpAgent;

[Collection(CliSpecsCollection.Name)]
public class when_launching_an_agent : given.a_builder_process
{
    [Cratis.Cli.for_DirectFileSecrets.given.unix_only.Fact]
    async Task should_filter_the_agent_environment_after_overrides()
    {
        var program = Script("agent", Check + " && touch clean; read -r request");
        var session = new BuildAgentSession(Folder, Folder, string.Empty, null, null, [], new(Folder, [], [], [], []), _ => { });
        await using var agent = new AcpAgent(session, "test", program, []);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await agent.Prompt("test", Overrides, deadline.Token);
        File.Exists(Path.Combine(Folder, "clean")).ShouldBeTrue();
    }
}
