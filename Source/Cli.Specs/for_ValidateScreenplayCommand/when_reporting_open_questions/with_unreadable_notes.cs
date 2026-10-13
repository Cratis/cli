// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reporting_open_questions;

public class with_unreadable_notes : given.a_model_with_questions
{
    void Establish() => File.WriteAllText(_notesPath, "<<<< unreadable");
    Task Because() => Run("screenplay", "validate", _root, "--open-questions", "report");
    [Fact] void should_report_invalid_input() => _exitCode.ShouldEqual(1);
    [Fact] void should_explain_the_read_failure() => _error.ShouldContain("UnreadableNotes");
}
