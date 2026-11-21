// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReplayObserverCommand.when_replaying_a_subscription_observer;

[Collection(CliSpecsCollection.Name)]
public class and_confirmation_is_not_given : given.a_replay_observer_command
{
    string? _prompt;

    void Establish() => _settings.ObserverId = SubscriptionObserverId;

    async Task Because()
    {
        _prompt = _command.Prompt(_settings);
        _result = await _command.ExecuteFromCommandLine(_settings);
    }

    [Fact] void should_warn_about_duplicate_inbox_events() => _prompt.ShouldContain("may append duplicate events to the consumer Inbox");
    [Fact] void should_name_the_explicit_producer_scope() => _prompt.ShouldContain("producer event store 'producer-store', namespace 'producer-namespace'");
    [Fact] void should_return_a_validation_error_before_connecting() => _result.ShouldEqual(ExitCodes.ValidationError);
}
