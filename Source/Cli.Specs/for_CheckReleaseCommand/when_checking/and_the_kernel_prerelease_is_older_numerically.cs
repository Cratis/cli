// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Host;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_kernel_prerelease_is_older_numerically : given.a_release_target
{
    async Task Because()
    {
        _release = _release with { MinimumKernelVersion = "19.181.3-rc.10" };
        _services.Server.GetVersionInfo().Returns(new ServerVersionInfo { Version = "19.181.3-rc.2", ProtocolVersion = "1" });
        await Check();
    }

    [Fact] void should_report_the_kernel_as_incompatible() => Kernel.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
}
