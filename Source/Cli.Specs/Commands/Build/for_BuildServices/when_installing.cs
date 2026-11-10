// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildServices;

public class when_installing : given.a_service_manager
{
    BuildServiceState _state;

    void Establish() => _answers["systemctl --user is-active svc.service"] = new(0, "active\n", string.Empty);

    async Task Because() => _state = await _services.Install(_definition, CancellationToken.None);

    [Fact] void should_write_the_unit() => File.ReadAllText(_platform.UnitPath("svc")).ShouldEqual(_platform.Render(_definition));
    [Fact] void should_start_it_after_writing_it() =>
        _ran.ToArray().ShouldEqual([.. _platform.Start("svc").Select(step => step.ToString()), "systemctl --user is-active svc.service"]);
    [Fact] void should_report_it_installed() => _state.Installed.ShouldBeTrue();
    [Fact] void should_report_it_running() => _state.Running.ShouldBeTrue();
}
