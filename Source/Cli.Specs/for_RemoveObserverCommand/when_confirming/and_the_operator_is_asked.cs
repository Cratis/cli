// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RemoveObserverCommand.when_confirming;

/// <summary>
/// Removal cannot be undone and reaches every namespace in the event store, so the prompt has to say what goes, what
/// stays and that a re-registered observer starts over - the distinction from a replay, which is what an operator
/// reaching for this may well have meant. A bare "are you sure?" gives no basis for answering.
/// </summary>
public class and_the_operator_is_asked : given.a_remove_observer_command
{
    string _prompt;

    void Because() => _prompt = _command.Confirmation(_settings);

    [Fact] void should_name_the_observer() => _prompt.ShouldContain("the-observer");
    [Fact] void should_name_the_event_store() => _prompt.ShouldContain("the-event-store");
    [Fact] void should_say_it_covers_every_namespace() => _prompt.ShouldContain("every namespace");
    [Fact] void should_say_read_models_are_left_alone() => _prompt.ShouldContain("Read models and their data are not touched");
    [Fact] void should_say_it_cannot_be_undone() => _prompt.ShouldContain("cannot be undone");
    [Fact] void should_say_a_re_registered_observer_starts_over() => _prompt.ShouldContain("start over");
}
