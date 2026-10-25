// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_rejecting_invalid_fixtures : given.a_conformance_run
{
    readonly List<AiTestReport> _invalid = [];
    async Task Because()
    {
        var valid = _fixture;
        AiTestCheck[][] invalidChecks = [
            [],
            [new() { Type = "file_exists", Path = "input.txt", Phase = AiTestPhase.Install }],
            [new() { Type = "file_exists", Path = "result.txt", Optional = true }],
            [new() { Type = "command", Phase = AiTestPhase.Install, Run = ["never-start"] }, .. valid.Checks],
            [new() { Type = "file_exists", Glob = "../*" }],
            [new() { Type = "file_exists", Glob = "/tmp/*" }],
            [new() { Type = "file_exists", Path = "../outside" }]
        ];
        foreach (var checks in invalidChecks)
        {
            _fixture = valid with { Checks = checks };
            _invalid.Add(await Run());
        }
        await File.WriteAllTextAsync(Path.Combine(_fixtureDirectory, "fixture.json"), "{\"name\":\"example\",\"checks\":[{\"phase\":\"unknown\"}]}");
        _invalid.Add(await _runner.Run(_project, _corpus, _settings, CancellationToken.None));
    }
    [Fact] void should_report_each_fixture_as_error() => _invalid.TrueForAll(report => report.Results.Single().Status == "error").ShouldBeTrue();
    [Fact] void should_return_failure_for_invalid_fixtures() => _invalid.TrueForAll(report => report.ExitCode == 1).ShouldBeTrue();
    [Fact] void should_reject_install_commands_explicitly() => _invalid[3].Results.Single().Reason.ShouldContain("Command checks are not allowed");
    [Fact] void should_not_start_the_harness() => _workspace.ShouldBeEmpty();
}
