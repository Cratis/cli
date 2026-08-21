// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiAgentEnvironment;

public class when_checking_if_non_interactive_is_requested : Specification
{
    [Fact] void should_be_requested_for_a_non_empty_value() =>
        AiAgentEnvironment.IsNonInteractiveRequested(name => name == AiAgentEnvironment.NonInteractiveEnvironmentVariable ? "1" : null).ShouldBeTrue();

    [Fact] void should_not_be_requested_for_an_empty_value() =>
        AiAgentEnvironment.IsNonInteractiveRequested(name => name == AiAgentEnvironment.NonInteractiveEnvironmentVariable ? string.Empty : null).ShouldBeFalse();
}
