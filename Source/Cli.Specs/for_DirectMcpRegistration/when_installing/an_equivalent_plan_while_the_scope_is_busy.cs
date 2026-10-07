// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class an_equivalent_plan_while_the_scope_is_busy : given.a_home_and_a_project
{
    Exception? _error;
    void Establish() => Install(DirectMcpScope.Project, ["claude"]);

    void Because()
    {
        using var held = DirectMcpManifest.AcquireLock(_project, _home);
        _error = Catch.Exception(() => _plan = Install(DirectMcpScope.Project, ["claude"]));
    }

    [Fact] void should_not_acquire_the_lock_for_a_no_op() => _error.ShouldBeNull();
    [Fact] void should_not_change_the_client() => _plan.Changes.ShouldBeEmpty();
    [Fact] void should_not_leave_a_project_lock() => File.Exists(ProjectFile(".cratis/direct-mcp.lock")).ShouldBeFalse();
    [Fact] void should_reuse_one_user_scope_lock() => Directory.GetFiles(HomeFile(".cratis/direct-mcp-locks")).Length.ShouldEqual(1);
}
