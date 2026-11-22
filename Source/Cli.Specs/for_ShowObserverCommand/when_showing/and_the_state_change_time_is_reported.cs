// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ShowObserverCommand.when_showing;

public class and_the_state_change_time_is_reported : given.a_show_observer_command
{
    string _json;

    void Establish() => _info.RunningStateChangedAt = DateTimeOffset.Parse("2026-10-10T14:00:00+02:00");

    async Task Because()
    {
        await Execute("json");
        _json = _output.ToString();
    }

    [Fact] void should_write_utc_iso_time_in_json() => _json.ShouldContain("\"runningStateChangedAt\": \"2026-10-10T12:00:00Z\"");
}
