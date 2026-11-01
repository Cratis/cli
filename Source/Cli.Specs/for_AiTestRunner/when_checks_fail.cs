// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_checks_fail : given.a_conformance_run
{
    void Establish()
    {
        _fixture = _fixture with
        {
            Checks = [
            new() { Type = "file_exists", Path = "missing" },
            new() { Type = "file_absent", Path = "result.txt" },
            new() { Type = "file_matches", Path = "result.txt", Pattern = "wrong" },
            new() { Type = "file_not_matches", Path = "result.txt", Pattern = "correct" },
            new() { Type = "command", Run = ["fake-command"], ExitCode = 1 }
        ]
        };
    }
    async Task Because() => _report = await Run();
    [Fact] void should_fail_every_check_type() => _report.Results.Single().Checks.All(check => check.Status == "failed").ShouldBeTrue();
    [Fact] void should_return_failure() => _report.ExitCode.ShouldEqual(1);
}
