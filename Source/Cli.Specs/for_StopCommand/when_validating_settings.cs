// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StopCommand;

public class when_validating_settings : Specification
{
    ValidationResult _foreign;
    ValidationResult _both;
    ValidationResult _named;
    ValidationResult _none;

    void Because()
    {
        _foreign = new StopSettings { Name = "postgres" }.Validate();
        _both = new StopSettings { Name = "cratis-stage-abc", All = true }.Validate();
        _named = new StopSettings { Name = "cratis-stage-abc" }.Validate();
        _none = new StopSettings().Validate();
    }

    [Fact] void should_refuse_a_container_that_is_not_a_sandbox() => _foreign.Successful.ShouldBeFalse();
    [Fact] void should_refuse_a_name_together_with_all() => _both.Successful.ShouldBeFalse();
    [Fact] void should_accept_a_sandbox_name() => _named.Successful.ShouldBeTrue();
    [Fact] void should_accept_no_name() => _none.Successful.ShouldBeTrue();
}
