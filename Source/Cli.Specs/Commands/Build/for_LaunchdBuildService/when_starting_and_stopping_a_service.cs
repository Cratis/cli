// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_LaunchdBuildService;

public class when_starting_and_stopping_a_service : Specification
{
    LaunchdBuildService _launchd;

    void Establish() => _launchd = new("/Users/someone", "501");

    [Fact] void should_unload_a_previous_version_before_loading() =>
        _launchd.Start("svc").Select(step => step.ToString()).ToArray().ShouldEqual([
            "launchctl bootout gui/501/io.cratis.svc",
            "launchctl bootstrap gui/501 /Users/someone/Library/LaunchAgents/io.cratis.svc.plist"]);
    [Fact] void should_accept_that_no_previous_version_is_loaded() => _launchd.Start("svc")[0].MayFail.ShouldBeTrue();
    [Fact] void should_require_the_load_to_succeed() => _launchd.Start("svc")[1].MayFail.ShouldBeFalse();
    [Fact] void should_unload_it_when_stopping() => _launchd.Stop("svc").Single().ToString().ShouldEqual("launchctl bootout gui/501/io.cratis.svc");
    [Fact] void should_report_running_when_launchd_says_so() =>
        _launchd.IsRunning(new(0, "gui/501/io.cratis.svc = {\n\tstate = running\n\tpid = 42\n}", string.Empty)).ShouldBeTrue();
    [Fact] void should_report_not_running_while_it_waits_to_restart() =>
        _launchd.IsRunning(new(0, "gui/501/io.cratis.svc = {\n\tstate = not running\n}", string.Empty)).ShouldBeFalse();
    [Fact] void should_report_not_running_when_it_is_not_loaded() =>
        _launchd.IsRunning(new(113, string.Empty, "Could not find service")).ShouldBeFalse();
}
