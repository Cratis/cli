// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_LinuxDirectSecrets;

public class when_looking_up_a_missing_secret : given.a_fake_secret_tool
{
    string? _secret = "unset";

    void Establish() => Behave("exit 1");

    async Task Because()
    {
        if (!OperatingSystem.IsWindows())
        {
            _secret = await new LinuxDirectSecrets(_tool).Read("ABC123", CancellationToken.None);
        }
    }

    [Fact] void should_report_nothing_stored() => (OperatingSystem.IsWindows() || _secret is null).ShouldBeTrue();
}
