// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_completeness;

public class and_the_selection_is_invalid : given.a_completeness_cli_process
{
    async Task Because() => await Run("--check", "unknown");

    /// <summary>
    /// Spectre settings validation returns -1, observed as 255 by a POSIX process.
    /// </summary>
    [Fact] void should_fail_with_a_usage_error() => _exitCode.ShouldEqual(OperatingSystem.IsWindows() ? -1 : 255);
    [Fact] void should_not_compile_the_model() => _error.ShouldBeEmpty();
    [Fact] void should_explain_the_accepted_selections() => string.Join(' ', _output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ShouldContain(new ValidateScreenplaySettings { Checks = ["unknown"] }.Validate().Message!);
    [Fact] void should_list_all_names() => new[] { "data-bindings", "input-surfaces", "field-origins", "query-keys", "event-consumers", "navigation", "all" }.All(name => _output.Contains(name, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_list_all_codes() => Enumerable.Range(530, 8).All(code => _output.Contains($"PLAY{code:D4}", StringComparison.Ordinal)).ShouldBeTrue();
}
