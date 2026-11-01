// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_overridden_open_questions : given.a_gate_with_questions
{
    Task Because() => Run("--open-questions", "report");
    [Fact] void should_report_without_failing() => _exitCode.ShouldEqual(0);
    [Fact] void should_still_report_questions() => Summary.GetProperty("openQuestions").GetProperty("findings").GetArrayLength().ShouldEqual(1);
    [Fact] void should_override_only_questions() => SourceOf("openQuestions").ShouldEqual("commandLine");
    [Fact] void should_preserve_file_warnings() => SourceOf("warningsAsErrors").ShouldEqual("file");
}
