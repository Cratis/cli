// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_SystemdBuildService;

public class when_starting_and_stopping_a_service : Specification
{
    SystemdBuildService _systemd;

    void Establish() => _systemd = new("/home/someone/.config");

    [Fact] void should_reload_enable_and_restart_it() =>
        _systemd.Start("svc").Select(step => step.ToString()).ToArray().ShouldEqual([
            "systemctl --user daemon-reload",
            "systemctl --user enable svc.service",
            "systemctl --user restart svc.service"]);
    [Fact] void should_require_every_start_step_to_succeed() => _systemd.Start("svc").Any(step => step.MayFail).ShouldBeFalse();
    [Fact] void should_disable_and_stop_it() => _systemd.Stop("svc").Single().ToString().ShouldEqual("systemctl --user disable --now svc.service");
    [Fact] void should_reload_once_its_file_is_gone() => _systemd.Forget("svc").Single().ToString().ShouldEqual("systemctl --user daemon-reload");
    [Fact] void should_report_running_when_active() => _systemd.IsRunning(new(0, "active\n", string.Empty)).ShouldBeTrue();
    [Fact] void should_report_not_running_when_failed() => _systemd.IsRunning(new(3, "failed\n", string.Empty)).ShouldBeFalse();
}
