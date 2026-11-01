// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reporting_open_questions;

public class with_report_mode : given.a_model_with_questions
{
    Task Because() => Run("screenplay", "validate", _root, "--open-questions", "report", "-o", "plain");
    [Fact] void should_remain_valid() => _exitCode.ShouldEqual(0);
    [Fact] void should_print_the_question() => _output.ShouldContain("model-note.open-question: Who may record?");
    [Fact] void should_print_the_count() => _output.ShouldContain("Model notes: 1 open question(s)");
}
