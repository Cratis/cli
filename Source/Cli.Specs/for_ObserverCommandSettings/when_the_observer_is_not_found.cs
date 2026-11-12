// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ObserverCommandSettings;

public class when_the_observer_is_not_found : given.observer_definitions
{
    void Establish() => _settings.ObserverId = "missing-observer";

    Task Because() => Resolve();

    [Fact] void should_keep_the_event_log_default() => _result.ShouldEqual("event-log");
}
