// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_missing_file : given.a_validation_gate_cli_process
{
    Task Because() => Run();
    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_use_default_checks() => SourceOf("checks").ShouldEqual("default");
    [Fact] void should_use_default_warnings_as_errors() => SourceOf("warningsAsErrors").ShouldEqual("default");
    [Fact] void should_use_default_scope() => SourceOf("scope").ShouldEqual("default");
    [Fact] void should_use_default_open_questions() => SourceOf("openQuestions").ShouldEqual("default");
    [Fact] void should_preserve_null_scope_value() => Gate.GetProperty("scope").GetProperty("value").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_preserve_null_questions_value() => Gate.GetProperty("openQuestions").GetProperty("value").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_select_checks() => Gate.GetProperty("checks").GetProperty("value").GetArrayLength().ShouldEqual(0);
}
