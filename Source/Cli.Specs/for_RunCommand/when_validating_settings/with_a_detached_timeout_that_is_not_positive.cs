// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_validating_settings;

public class with_a_detached_timeout_that_is_not_positive : Specification
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    void should_reject_the_timeout(int seconds)
    {
        var result = new RunSettings { Detach = true, Timeout = seconds }.Validate();
        result.Successful.ShouldBeFalse();
        result.Message.ShouldContain("--timeout");
    }
}
