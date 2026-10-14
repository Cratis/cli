// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reporting_open_questions;

public class with_an_invalid_mode : given.a_model_with_questions
{
    Task Because() => Run("screenplay", "validate", _root, "--open-questions", "invalid");
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(5);
    [Fact] void should_explain_the_valid_modes() => _output.ShouldContain("--open-questions requires report or fail");
}
