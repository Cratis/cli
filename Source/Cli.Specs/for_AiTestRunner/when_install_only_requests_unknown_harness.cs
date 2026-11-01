// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiTestRunner;

public class when_install_only_requests_unknown_harness : given.a_conformance_run
{
    void Establish()
    {
        _settings.InstallOnly = true;
        _settings.Harnesses = "unsupported";
    }

    async Task Because() => _report = await Run();

    [Fact] void should_skip_unknown_harness() => _report.Results.Single().Status.ShouldEqual("skipped");
    [Fact] void should_explain_skip() => _report.Results.Single().Reason.ShouldContain("Unsupported harness: unsupported");
    [Fact] void should_not_run_install_checks() => _report.Results.Single().Checks.ShouldBeEmpty();
    [Fact] void should_exit_zero_like_normal_mode() => _report.ExitCode.ShouldEqual(0);
}
