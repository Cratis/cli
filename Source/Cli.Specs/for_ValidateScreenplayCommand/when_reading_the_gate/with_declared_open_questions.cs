// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_declared_open_questions : given.a_gate_with_questions
{
    Task Because() => Run();
    [Fact] void should_gate_on_questions_from_the_model_root() => _exitCode.ShouldEqual(5);
    [Fact] void should_report_the_root_question_for_a_nested_file_target() => Summary.GetProperty("openQuestions").GetProperty("findings")[0].GetProperty("message").GetString().ShouldEqual("Who may record?");
    [Fact] void should_attribute_questions_to_the_file() => SourceOf("openQuestions").ShouldEqual("file");
}
