// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_completeness;

public class and_the_selection_is_invalid : given.a_completeness_cli_process
{
    async Task Because() => await Run("--check", "unknown");

    [Fact] void should_fail_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_compile_the_model() => _output.ShouldBeEmpty();
    [Fact] void should_name_the_unknown_selection() => _error.ShouldContain("unknown");
    [Fact] void should_report_a_validation_error() => _error.ShouldContain(ExitCodes.ValidationErrorCode);
    [Fact] void should_list_all_names() => new[] { "data-bindings", "input-surfaces", "field-origins", "query-keys", "event-consumers", "navigation", "personas", "purposes", "privilege", "enum-pinning", "all" }.All(name => _error.Contains(name, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_list_all_codes() => Enumerable.Range(530, 8).Concat([574, 575, 576, 602, 603, 604, 605, 606, 652, 671]).All(code => _error.Contains($"PLAY{code:D4}", StringComparison.Ordinal)).ShouldBeTrue();
}
