// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class one_client_again_in_a_different_order : given.a_home_and_a_project
{
    byte[] _manifest;
    Exception? _error;

    void Establish()
    {
        Install(DirectMcpScope.Project, ["claude", "codex"]);
        _manifest = File.ReadAllBytes(ProjectFile(DirectMcpManifest.RelativePath));
    }

    void Because()
    {
        using var held = DirectMcpManifest.AcquireLock(_project, _home);
        _error = Catch.Exception(() => _plan = Install(DirectMcpScope.Project, ["claude"]));
    }

    [Fact] void should_not_take_the_busy_lock_for_a_no_op() => _error.ShouldBeNull();
    [Fact] void should_plan_no_client_changes() => _plan.Changes.ShouldBeEmpty();
    [Fact] void should_leave_the_manifest_bytes_unchanged() => File.ReadAllBytes(ProjectFile(DirectMcpManifest.RelativePath)).ShouldEqual(_manifest);
}
