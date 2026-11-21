// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReplayObserverCommand.when_replaying_a_subscription_observer;

[Collection(CliSpecsCollection.Name)]
public class and_scope_is_not_explicit : given.a_replay_observer_command
{
    void Establish()
    {
        _settings.ObserverId = SubscriptionObserverId;
        _settings.Namespace = null;
        _settings.Yes = true;
    }

    async Task Because() => _result = await _command.ExecuteFromCommandLine(_settings);

    [Fact] void should_return_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
}
