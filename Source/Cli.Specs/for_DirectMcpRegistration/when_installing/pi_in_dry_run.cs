// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class pi_in_dry_run : given.a_home_and_a_project
{
    void Because() => _plan = Install(DirectMcpScope.User, ["pi"], dryRun: true);
    [Fact] void should_plan_the_member() => _plan.Changes.Single().Client.ShouldEqual("pi");
    [Fact] void should_write_nothing() => Directory.GetFiles(_home, "*", SearchOption.AllDirectories).ShouldBeEmpty();
}
