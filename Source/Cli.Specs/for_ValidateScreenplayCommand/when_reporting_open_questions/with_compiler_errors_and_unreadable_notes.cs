// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reporting_open_questions;

public class with_compiler_errors_and_unreadable_notes : given.a_model_with_questions
{
    void Establish()
    {
        File.WriteAllText(_notesPath, "<<<< unreadable");
        File.WriteAllText(_document, "module M\n  feature F\n    slice StateChange Record\n      command Record\n        value UnknownType\n");
    }
    Task Because() => Run("screenplay", "validate", _root, "--open-questions", "fail");
    [Fact] void should_preserve_compiler_error_exit_code() => _exitCode.ShouldEqual(5);
}
