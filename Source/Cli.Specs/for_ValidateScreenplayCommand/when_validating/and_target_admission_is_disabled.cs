// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

[Collection(CliSpecsCollection.Name)]
public class and_target_admission_is_disabled : given.a_reporting_validate_screenplay_command
{
    int _result;
    JsonElement _summary;

    void Establish()
    {
        _settings.Executable = true;
        _settings.Target = "cratis";
        _settings.NoRenderAdmission = true;
        _validation.ValidateExecutable(Arg.Any<string>()).Returns(new ValidatedScreenplay(1, []) { Executable = true });
    }

    async Task Because()
    {
        _result = await Execute();
        _summary = JsonSerializer.Deserialize<JsonElement>(_output.ToString());
    }

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_keep_the_successful_binding() => _summary.GetProperty("binding").GetProperty("executable").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_skipped_target_admission() => _summary.GetProperty("targetAdmission").GetProperty("status").GetString().ShouldEqual("skipped");
    [Fact] void should_report_a_valid_result() => _summary.GetProperty("valid").GetBoolean().ShouldBeTrue();
    [Fact] void should_leave_standard_error_empty() => _error.ToString().ShouldBeEmpty();
    [Fact] void should_bind_the_model() => _validation.Received(1).ValidateExecutable(_folder);
}
