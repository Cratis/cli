// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class without_a_client : given.a_home_and_a_project
{
    void Establish()
    {
        Write(HomeFile(".claude.json"), "{}");
        Directory.CreateDirectory(HomeFile(".cursor"));
    }

    void Because() => _plan = Install(DirectMcpScope.User, []);

    [Fact] void should_register_the_clients_whose_configuration_exists() => _plan.Changes.Select(change => change.Client).ShouldContainOnly("claude", "cursor");
}
