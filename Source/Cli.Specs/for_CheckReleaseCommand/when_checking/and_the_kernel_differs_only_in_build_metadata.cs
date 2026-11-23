// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Host;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_kernel_differs_only_in_build_metadata : given.a_release_target
{
    async Task Because()
    {
        _release = _release with { MinimumKernelVersion = "1.0.0+b2" };
        _services.Server.GetVersionInfo().Returns(new ServerVersionInfo { Version = "1.0.0+b1", ProtocolVersion = "1" });
        await Check();
    }

    [Fact] void should_ignore_the_build_metadata() => Kernel.Verdict.ShouldEqual(CompatibilityVerdict.Compatible);
}
