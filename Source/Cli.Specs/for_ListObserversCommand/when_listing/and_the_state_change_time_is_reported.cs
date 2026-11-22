// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_ListObserversCommand.when_listing;

public class and_the_state_change_time_is_reported : given.a_list_observers_command
{
    string _json;
    string _plain;

    void Establish() => _data.Add(new ObserverInformation { Id = "obs-1", RunningStateChangedAt = DateTimeOffset.Parse("2026-10-10T14:00:00+02:00") });

    async Task Because()
    {
        await Execute("json");
        _json = _output.ToString();
        _output.GetStringBuilder().Clear();
        await Execute("plain");
        _plain = _output.ToString();
    }

    [Fact] void should_write_utc_iso_time_in_json() => _json.ShouldContain("\"runningStateChangedAt\": \"2026-10-10T12:00:00Z\"");
    [Fact] void should_write_utc_iso_time_in_plain() => _plain.ShouldContain("2026-10-10T12:00:00Z");
}
