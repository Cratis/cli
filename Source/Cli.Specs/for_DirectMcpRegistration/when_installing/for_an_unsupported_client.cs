// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class for_an_unsupported_client : given.a_home_and_a_project
{
    void Establish() => _environment["CODEX_HOME"] = Path.Combine(Path.GetTempPath(), "elsewhere-codex");

    void Because() => _plan = Install(DirectMcpScope.User, ["pi", "codex"]);

    [Fact] void should_report_pi() => _plan.Unsupported.ShouldContain(reason => reason.StartsWith("pi:", StringComparison.Ordinal));
    [Fact] void should_report_codex_relocated_outside_the_home_directory() => _plan.Unsupported.ShouldContain(reason => reason.StartsWith("codex:", StringComparison.Ordinal) && reason.Contains("CODEX_HOME"));
    [Fact] void should_report_that_nothing_could_be_registered() => _plan.NothingRegistrable.ShouldBeTrue();
    [Fact] void should_write_nothing() => Directory.GetFileSystemEntries(_home).ShouldBeEmpty();
}
