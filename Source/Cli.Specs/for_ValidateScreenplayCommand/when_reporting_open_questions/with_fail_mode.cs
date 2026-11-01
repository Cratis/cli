// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reporting_open_questions;

public class with_fail_mode : given.a_model_with_questions
{
    Task Because() => Run("screenplay", "validate", _root, "--open-questions", "fail", "-o", "json-compact");
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(5);
    [Fact] void should_include_findings_in_json() => JsonDocument.Parse(_output).RootElement.GetProperty("openQuestions").GetProperty("findings")[0].GetProperty("message").GetString().ShouldEqual("Who may record?");
    [Fact] void should_mark_the_result_invalid() => JsonDocument.Parse(_output).RootElement.GetProperty("valid").GetBoolean().ShouldBeFalse();
}
