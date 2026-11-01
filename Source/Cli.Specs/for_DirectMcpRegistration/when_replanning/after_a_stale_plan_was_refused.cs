// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_replanning;

public class after_a_stale_plan_was_refused : given.a_home_and_a_project
{
    void Establish()
    {
        _plan = DirectMcpRegistration.Install(DirectMcpScope.User, Locations, ["cursor"], DirectMcpClients.Arguments(Origin, null));
        Install(DirectMcpScope.User, ["claude"]);
        Catch.Exception(() => _plan.Apply(AiFileOperations.Performing));
    }

    void Because() => Install(DirectMcpScope.User, ["cursor"]);

    [Fact] void should_own_both_clients() => DirectMcpManifest.Read(_home).Servers.Select(server => server.Harness).ShouldContainOnly("claude", "cursor");
}
