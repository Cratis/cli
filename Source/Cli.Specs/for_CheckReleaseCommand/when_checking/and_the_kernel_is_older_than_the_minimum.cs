// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Host;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_kernel_is_older_than_the_minimum : given.a_release_target
{
    async Task Because()
    {
        _services.Server.GetVersionInfo().Returns(new ServerVersionInfo { Version = "19.100.0", ProtocolVersion = "1" });
        await Check();
    }

    [Fact] void should_report_the_kernel_as_incompatible() => Kernel.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
    [Fact] void should_name_the_required_version_in_the_action() => Kernel.Action.ShouldContain("19.157.0");
    [Fact] void should_still_check_the_other_aspects() => Contracts.Verdict.ShouldEqual(CompatibilityVerdict.Compatible);
    [Fact] void should_be_incompatible() => _result.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
    [Fact] void should_exit_with_validation_error() => _result.ExitCode.ShouldEqual(ExitCodes.ValidationError);
}
