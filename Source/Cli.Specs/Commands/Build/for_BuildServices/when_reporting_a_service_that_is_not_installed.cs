// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildServices;

public class when_reporting_a_service_that_is_not_installed : given.a_service_manager
{
    BuildServiceState _state;

    async Task Because() => _state = await _services.Status("svc", CancellationToken.None);

    [Fact] void should_report_it_not_installed() => _state.Installed.ShouldBeFalse();
    [Fact] void should_report_it_not_running() => _state.Running.ShouldBeFalse();
    [Fact] void should_name_the_manager() => _state.Manager.ShouldEqual("systemd");
}
