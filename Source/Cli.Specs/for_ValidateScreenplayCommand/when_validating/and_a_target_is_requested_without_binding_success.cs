// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

[Collection(CliSpecsCollection.Name)]
public class and_a_target_is_requested_without_binding_success : given.a_reporting_validate_screenplay_command
{
    int _result;
    JsonElement _summary;

    void Establish()
    {
        _settings.Executable = true;
        _settings.Target = "cratis";
        _validation.ValidateExecutable(Arg.Any<string>()).Returns(new ValidatedScreenplay(1, []) { Executable = false });
    }

    async Task Because()
    {
        _result = await Execute();
        _summary = JsonSerializer.Deserialize<JsonElement>(_output.ToString());
    }

    [Fact] void should_fail_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_binding_failure() => _error.ToString().ShouldContain("The model does not bind into an executable semantic model");
    [Fact] void should_still_write_the_target_summary() => _summary.GetProperty("valid").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_skipped_target_admission() => _summary.GetProperty("targetAdmission").GetProperty("status").GetString().ShouldEqual("skipped");
    [Fact] void should_preserve_the_executable_check() => _summary.GetProperty("checked").GetString().ShouldEqual("executable");
}
