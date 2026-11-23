// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Host;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_kernel_is_a_prerelease_of_the_minimum : given.a_release_target
{
    async Task Because()
    {
        _release = _release with { MinimumKernelVersion = "1.0.0" };
        _services.Server.GetVersionInfo().Returns(new ServerVersionInfo { Version = "1.0.0-alpha", ProtocolVersion = "1" });
        await Check();
    }

    [Fact] void should_report_the_kernel_as_incompatible() => Kernel.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
}
