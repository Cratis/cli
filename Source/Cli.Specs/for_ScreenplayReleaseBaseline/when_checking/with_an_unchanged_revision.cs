// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_checking;

public class with_an_unchanged_revision : given.a_release_model
{
    void Establish() => ReleaseBaselineCommands.Admit(_root, "v1").ExitCode.ShouldEqual(0);
    Task Because() => Run("screenplay", "baseline", "check", _root);
    [Fact] void should_be_clean() => _exitCode.ShouldEqual(0);
    [Fact] void should_not_report_a_failure() => _error.ShouldBeEmpty();
}
