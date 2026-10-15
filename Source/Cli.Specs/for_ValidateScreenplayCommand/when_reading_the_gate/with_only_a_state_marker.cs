// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_only_a_state_marker : given.a_gate_with_questions
{
    void Establish() => File.Delete(_gatePath);
    Task Because() => Run("--open-questions", "fail");
    [Fact] void should_preserve_flag_only_notes_from_the_target_folder() => _exitCode.ShouldEqual(0);
    [Fact] void should_not_read_parent_questions_without_a_declared_gate_or_bypass() => Summary.GetProperty("openQuestions").GetProperty("findings").GetArrayLength().ShouldEqual(0);
    [Fact] void should_still_report_the_resolved_root_in_the_gate() => Gate.GetProperty("modelRoot").GetString().ShouldEqual(_root);
}
