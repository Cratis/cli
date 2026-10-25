// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_running_isolated_checks : given.a_conformance_run
{
    Dictionary<string, string> _before = [];

    void Establish()
    {
        _before = Directory.GetFiles(_project, "*", SearchOption.AllDirectories).ToDictionary(path => path, Snapshot);
        _fixture = _fixture with { Checks = [
            new() { Type = "file_exists", Path = ".cratis/ai/rules/general.md", Phase = AiTestPhase.Install },
            new() { Type = "file_exists", Path = "input.txt", Phase = AiTestPhase.Install },
            new() { Type = "file_absent", Path = "result.txt", Phase = AiTestPhase.Install },
            new() { Type = "file_exists", Path = "result.txt" },
            new() { Type = "file_absent", Glob = "*.forbidden" },
            new() { Type = "file_matches", Path = "result.txt", Pattern = "correct" },
            new() { Type = "file_not_matches", Glob = "*.txt", Pattern = "wrong" },
            new() { Type = "command", Run = ["fake-command"] }
        ] };
    }

    static string Snapshot(string path) => new FileInfo(path).LinkTarget ?? File.ReadAllText(path);

    async Task Because() => _report = await Run();

    [Fact] void should_pass_every_check_type() => _report.Results.Single().Checks.All(check => check.Status == "passed").ShouldBeTrue();
    [Fact] void should_succeed() => _report.ExitCode.ShouldEqual(0);
    [Fact] void should_leave_invoking_repository_unchanged() => _before.All(pair => Snapshot(pair.Key) == pair.Value).ShouldBeTrue();
    [Fact] void should_not_add_files_to_invoking_repository() => Directory.GetFiles(_project, "*", SearchOption.AllDirectories).Length.ShouldEqual(_before.Count);
    [Fact] void should_clean_the_workspace() => Directory.Exists(_workspace).ShouldBeFalse();
    [Fact] void should_not_retain_transcripts_by_default() => _report.Results.Single().TranscriptPath.ShouldBeNull();
    [Fact] void should_report_installed_hashes() => _report.Results.Single().Installed.All(file => !string.IsNullOrEmpty(file.Hash)).ShouldBeTrue();
    [Fact] void should_not_infer_instruction_loads() => _report.Results.Single().ObservedLoad.Single().Status.ShouldEqual("unknown");
    [Fact] void should_not_claim_memory_isolation() => _report.Results.Single().UserMemoryIsolation.ShouldEqual("unknown");
    [Fact] void should_serialize_the_report_contract() => JsonSerializer.Serialize(_report, AiTestFixture.Json).ShouldContain("\"corpus_revision\"");
}
