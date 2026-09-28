// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_LinuxDirectSecrets;

public class when_the_secret_service_fails_during_lookup : given.a_fake_secret_tool
{
    Exception? _error;

    void Establish() => Behave("echo 'Cannot autolaunch D-Bus without X11 display' >&2\nexit 1");

    async Task Because()
    {
        if (!OperatingSystem.IsWindows())
        {
            _error = await Catch.Exception(() => new LinuxDirectSecrets(_tool).Read("ABC123", CancellationToken.None));
        }
    }

    [Fact] void should_fail_instead_of_reporting_nothing_stored() => (OperatingSystem.IsWindows() || _error is DirectAuthError).ShouldBeTrue();
    [Fact] void should_not_show_the_tool_diagnostics() => (_error?.Message ?? string.Empty).ShouldNotContain("D-Bus");
}
