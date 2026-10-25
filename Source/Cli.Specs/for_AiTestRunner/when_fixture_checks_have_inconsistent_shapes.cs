// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai.Testing;

namespace Cratis.Cli.for_AiTestRunner;

public class when_fixture_checks_have_inconsistent_shapes : given.a_conformance_run
{
    readonly Dictionary<string, string?> _reasons = [];

    async Task Because()
    {
        var valid = _fixture;
        Dictionary<string, AiTestCheck> invalid = new()
        {
            ["both"] = new() { Type = "file_exists", Path = "a", Glob = "*" },
            ["neither"] = new() { Type = "file_exists" },
            ["pattern_missing"] = new() { Type = "file_matches", Path = "a" },
            ["not_pattern_missing"] = new() { Type = "file_not_matches", Path = "a" },
            ["run_missing"] = new() { Type = "command" },
            ["run_on_file"] = new() { Type = "file_exists", Path = "a", Run = ["x"] },
            ["exit_code_on_file"] = new() { Type = "file_exists", Path = "a", ExitCode = 0 },
            ["path_on_command"] = new() { Type = "command", Run = ["x"], Path = "a" },
            ["glob_on_command"] = new() { Type = "command", Run = ["x"], Glob = "*" },
            ["pattern_on_command"] = new() { Type = "command", Run = ["x"], Pattern = "p" },
            ["pattern_on_exists"] = new() { Type = "file_exists", Path = "a", Pattern = "p" },
            ["pattern_on_absent"] = new() { Type = "file_absent", Path = "a", Pattern = "p" }
        };
        foreach (var (name, check) in invalid)
        {
            _fixture = valid with { Checks = [.. valid.Checks, check] };
            var result = (await Run()).Results.Single();
            _reasons[name] = result.Status == "error" ? result.Reason : $"unexpected {result.Status}";
        }
    }

    [Fact] void should_refuse_path_and_glob_together() => _reasons["both"].ShouldContain("exactly one path or glob");
    [Fact] void should_refuse_neither_path_nor_glob() => _reasons["neither"].ShouldContain("exactly one path or glob");
    [Fact] void should_require_pattern_for_file_matches() => _reasons["pattern_missing"].ShouldContain("requires pattern");
    [Fact] void should_require_pattern_for_file_not_matches() => _reasons["not_pattern_missing"].ShouldContain("requires pattern");
    [Fact] void should_require_run_for_command() => _reasons["run_missing"].ShouldContain("requires run");
    [Fact] void should_refuse_run_on_file_check() => _reasons["run_on_file"].ShouldContain("must not declare run or exit_code");
    [Fact] void should_refuse_exit_code_on_file_check() => _reasons["exit_code_on_file"].ShouldContain("must not declare run or exit_code");
    [Fact] void should_refuse_path_on_command() => _reasons["path_on_command"].ShouldContain("must not declare path, glob or pattern");
    [Fact] void should_refuse_glob_on_command() => _reasons["glob_on_command"].ShouldContain("must not declare path, glob or pattern");
    [Fact] void should_refuse_pattern_on_command() => _reasons["pattern_on_command"].ShouldContain("must not declare path, glob or pattern");
    [Fact] void should_refuse_pattern_on_file_exists() => _reasons["pattern_on_exists"].ShouldContain("must not declare pattern");
    [Fact] void should_refuse_pattern_on_file_absent() => _reasons["pattern_on_absent"].ShouldContain("must not declare pattern");
}
