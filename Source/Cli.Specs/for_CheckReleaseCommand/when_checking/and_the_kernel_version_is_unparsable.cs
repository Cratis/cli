// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Host;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_kernel_version_is_unparsable : given.a_release_target
{
    async Task Because()
    {
        _services.Server.GetVersionInfo().Returns(new ServerVersionInfo { Version = "banana", ProtocolVersion = "1" });
        await Check();
    }

    [Fact] void should_report_the_kernel_as_unknown() => Kernel.Verdict.ShouldEqual(CompatibilityVerdict.Unknown);
}
