// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_validating_installation : given.a_conformance_run
{
    Exception? _modified;
    Exception? _missing;
    AiConfiguration _sourceMode = null!;
    AiTestReport _usage = null!;
    AiTestReport _empty = null!;

    void Because()
    {
        File.WriteAllText(Installed("general.md"), "locally modified");
        _modified = Record.Exception(() => AiTestRunner.Configuration(_project, _corpus, corpusMode: false));
        _sourceMode = AiTestRunner.Configuration(_project, _corpus, corpusMode: true);
        File.Delete(Path.Combine(_project, ".cratis", "ai.manifest.json"));
        _missing = Record.Exception(() => AiTestRunner.Configuration(_project, _corpus, corpusMode: false));
        _usage = new([], Error: "usage error");
        _empty = AiTestReport.Create([]);
    }
    [Fact] void should_reject_modified_managed_files() => _modified!.Message.ShouldContain("modified");
    [Fact] void should_reject_missing_installation() => _missing!.Message.ShouldContain("manifest");
    [Fact] void should_allow_source_mode() => _sourceMode.Profiles.Single().ShouldEqual("cratis/example");
    [Fact] void should_exit_two_for_usage_errors() => _usage.ExitCode.ShouldEqual(2);
    [Fact] void should_exit_zero_when_no_fixtures_ran() => _empty.ExitCode.ShouldEqual(0);
    [Fact] void should_warn_when_no_fixtures_ran() => _empty.Warning.ShouldContain("no fixtures ran");
}
