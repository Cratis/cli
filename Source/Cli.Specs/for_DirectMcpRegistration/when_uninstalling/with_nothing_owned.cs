// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class with_nothing_owned : given.a_home_and_a_project
{
    void Because() => _plan = Uninstall(DirectMcpScope.Project);

    [Fact] void should_plan_no_changes() => _plan.Changes.ShouldBeEmpty();
    [Fact] void should_not_create_project_metadata() => Directory.Exists(ProjectFile(".cratis")).ShouldBeFalse();
    [Fact] void should_not_create_a_user_scope_lock() => Directory.Exists(HomeFile(".cratis")).ShouldBeFalse();
}
