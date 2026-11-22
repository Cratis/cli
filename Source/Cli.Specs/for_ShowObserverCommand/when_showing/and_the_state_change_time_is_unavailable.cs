// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ShowObserverCommand.when_showing;

public class and_the_state_change_time_is_unavailable : given.a_show_observer_command
{
    string _json;

    async Task Because()
    {
        await Execute("json");
        _json = _output.ToString();
    }

    [Fact] void should_omit_the_property_in_json() => _json.ShouldNotContain("runningStateChangedAt");
}
