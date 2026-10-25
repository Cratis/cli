// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiTestRunner;

public class when_install_only_has_no_install_checks : given.a_conformance_run
{
    void Establish() => _settings.InstallOnly = true;

    async Task Because() => _report = await Run();

    [Fact] void should_skip() => _report.Results.Single().Status.ShouldEqual("skipped");
    [Fact] void should_explain_skip() => _report.Results.Single().Reason.ShouldEqual("no install-phase checks");
    [Fact] void should_mark_result_phase_not_run() => _report.Results.Single().ResultPhase.ShouldEqual("not_run");
    [Fact] void should_warn_that_nothing_passed() => _report.Warning.ShouldContain("no fixtures ran");
}
