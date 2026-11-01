// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reporting_open_questions;

public class with_warnings_as_errors : given.a_model_with_questions
{
    Task Because() => Run("screenplay", "validate", _root, "--open-questions", "report", "--warnings-as-errors");
    [Fact] void should_not_treat_questions_as_compiler_warnings() => _exitCode.ShouldEqual(0);
}
