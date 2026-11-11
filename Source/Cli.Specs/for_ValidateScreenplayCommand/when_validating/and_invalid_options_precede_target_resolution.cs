// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

[Collection(CliSpecsCollection.Name)]
public class and_invalid_options_precede_target_resolution : given.a_reporting_validate_screenplay_command
{
    int _result;

    void Establish()
    {
        _settings.OpenQuestions = ["invalid"];
        _settings.Checks = ["invalid"];
        _settings.Target = "invalid";
        _settings.Path = "missing.play";
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_first_invalid_option() => _error.ToString().ShouldContain("--open-questions requires report or fail and cannot be repeated.");
    [Fact] void should_not_report_later_failures() => _error.ToString().ShouldNotContain("Unknown --check");
    [Fact] void should_not_produce_a_summary() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_not_call_validation() => _validation.ReceivedCalls().ShouldBeEmpty();
}
