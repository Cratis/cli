// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_everything_is_compatible : given.a_release_target
{
    Task Because() => Check();

    [Fact] void should_be_compatible() => _result.Verdict.ShouldEqual(CompatibilityVerdict.Compatible);
    [Fact] void should_exit_with_success() => _result.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_exactly_the_three_checks_in_order() => _result.Checks.Select(_ => _.Check).ShouldEqual([CompatibilityCheck.Kernel, CompatibilityCheck.Contracts, CompatibilityCheck.Schemas]);
    [Fact] void should_not_ask_for_any_action() => _result.Checks.All(_ => _.Action.Length == 0).ShouldBeTrue();
}
