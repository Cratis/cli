// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GlobalSettings.when_checking_if_environment_is_interactive;

public class all_combinations : Specification
{
    [Fact] void should_not_be_interactive_when_nothing_supports_interaction() => Result(false, false, false, false).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_only_non_interactive_is_requested() => Result(false, false, false, true).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_only_an_ai_agent_is_detected() => Result(false, false, true, false).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_an_ai_agent_is_detected_and_non_interactive_is_requested() => Result(false, false, true, true).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_only_output_is_a_terminal() => Result(false, true, false, false).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_output_is_a_terminal_and_non_interactive_is_requested() => Result(false, true, false, true).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_output_is_a_terminal_and_an_ai_agent_is_detected() => Result(false, true, true, false).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_output_is_a_terminal_and_both_non_interactive_signals_are_present() => Result(false, true, true, true).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_only_interaction_is_supported() => Result(true, false, false, false).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_interaction_is_supported_and_non_interactive_is_requested() => Result(true, false, false, true).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_interaction_is_supported_and_an_ai_agent_is_detected() => Result(true, false, true, false).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_interaction_is_supported_and_both_non_interactive_signals_are_present() => Result(true, false, true, true).ShouldBeFalse();
    [Fact] void should_be_interactive_when_interaction_and_terminal_output_are_available() => Result(true, true, false, false).ShouldBeTrue();
    [Fact] void should_not_be_interactive_when_everything_is_available_but_non_interactive_is_requested() => Result(true, true, false, true).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_everything_is_available_but_an_ai_agent_is_detected() => Result(true, true, true, false).ShouldBeFalse();
    [Fact] void should_not_be_interactive_when_everything_is_available_and_both_non_interactive_signals_are_present() => Result(true, true, true, true).ShouldBeFalse();

    static bool Result(bool interactionSupported, bool outputIsTerminal, bool aiAgentDetected, bool nonInteractiveRequested) =>
        GlobalSettings.IsInteractiveEnvironment(interactionSupported, outputIsTerminal, aiAgentDetected, nonInteractiveRequested);
}
