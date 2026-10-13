// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reporting_open_questions;

public class with_repeated_modes : given.a_model_with_questions
{
    Task Because() => Run("screenplay", "validate", _root, "--open-questions", "report", "--open-questions", "fail");
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(5);
    [Fact] void should_explain_the_repeat() => _error.ShouldContain("cannot be repeated");
}
