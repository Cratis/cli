// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_validating_settings;

public class with_a_timeout_that_is_not_positive : Specification
{
    ValidationResult _result;

    void Because() => _result = new RunSettings { Timeout = 0 }.Validate();

    [Fact] void should_be_invalid() => _result.Successful.ShouldBeFalse();
    [Fact] void should_name_the_option() => _result.Message.ShouldContain("--timeout");
}
