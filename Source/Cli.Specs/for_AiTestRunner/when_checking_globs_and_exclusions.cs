// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_checking_globs_and_exclusions : given.a_conformance_run
{
    void Establish()
    {
        foreach (var directory in new[] { ".git", ".cratis", "node_modules", "nested/node_modules", "nested/deeper" })
        {
            var input = Path.Combine(_fixtureDirectory, "input", directory);
            Directory.CreateDirectory(input);
            File.WriteAllText(Path.Combine(input, directory == "nested/deeper" ? "match.txt" : "secret.hidden"), "first\nsecond");
        }
        _fixture = _fixture with { Checks = [
            new() { Type = "file_exists", Glob = ".cratis/**/*.md", Phase = AiTestPhase.Install },
            new() { Type = "file_absent", Glob = "**/*.hidden" },
            new() { Type = "file_matches", Glob = "**/match.txt", Pattern = "second" },
            new() { Type = "file_matches", Glob = "*.txt", Pattern = "original|correct" },
            new() { Type = "file_matches", Glob = "**/*.nonexistent", Pattern = "anything" },
            new() { Type = "file_not_matches", Glob = "**/match.txt", Pattern = "^second" },
            new() { Type = "file_absent", Path = ".cratis/secret.hidden" }
        ] };
    }
    async Task Because() => _report = await Run();
    [Fact] void should_allow_explicit_installed_tree_checks() => _report.Results.Single().Checks[0].Status.ShouldEqual("passed");
    [Fact] void should_exclude_result_metadata_and_dependencies() => _report.Results.Single().Checks[1].Status.ShouldEqual("passed");
    [Fact] void should_match_across_directories() => _report.Results.Single().Checks[2].Status.ShouldEqual("passed");
    [Fact] void should_match_only_root_files_without_double_star() => _report.Results.Single().Checks[3].Status.ShouldEqual("passed");
    [Fact] void should_not_pass_zero_matches() => _report.Results.Single().Checks[4].Evidence.ShouldEqual("zero files matched");
    [Fact] void should_leave_multiline_mode_off() => _report.Results.Single().Checks[5].Status.ShouldEqual("passed");
    [Fact] void should_exclude_explicit_result_metadata_paths() => _report.Results.Single().Checks[6].Status.ShouldEqual("passed");
}
