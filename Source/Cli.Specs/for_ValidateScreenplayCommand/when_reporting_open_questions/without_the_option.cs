// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reporting_open_questions;

public class without_the_option : given.a_model_with_questions
{
    void Establish() => File.WriteAllText(_notesPath, "<<<< unreadable");
    Task Because() => Run("screenplay", "validate", _root);
    [Fact] void should_ignore_unreadable_notes() => _exitCode.ShouldEqual(0);
    [Fact] void should_not_report_questions() => _output.ShouldNotContain("Model notes:");
    [Fact] void should_not_report_a_notes_failure() => _output.ShouldNotContain("UnreadableNotes");
}
