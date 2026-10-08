// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_completeness;

public class and_selections_are_repeated : given.a_completeness_cli_process
{
    async Task Because() => await Run("--check", "navigation", "--check", "PLAY0536,navigation");

    [Fact] void should_succeed_despite_warnings() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_both_selected_findings() => new[] { "PLAY0536", "PLAY0537" }.All(code => _error.Contains(code, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_union_without_duplicates() => JsonSerializer.Deserialize<JsonElement>(_output).GetProperty("checks").EnumerateArray().Select(_ => _.GetString()).ShouldContainOnly("event-consumers", "navigation");
    [Fact] void should_report_that_checks_ran() => JsonSerializer.Deserialize<JsonElement>(_output).GetProperty("completenessStatus").GetString().ShouldEqual("ran");
    [Fact] void should_not_report_duplicate_findings() => JsonSerializer.Deserialize<JsonElement>(_output).GetProperty("diagnostics").GetInt32().ShouldEqual(2);
}
