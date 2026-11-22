// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_ListObserversCommand.when_listing;

public class and_the_state_change_time_is_unavailable : given.a_list_observers_command
{
    string _json;
    string _plain;
    string _quiet;

    void Establish() => _data.Add(new ObserverInformation { Id = "obs-1" });

    async Task Because()
    {
        await Execute("json");
        _json = _output.ToString();
        _output.GetStringBuilder().Clear();
        await Execute("plain");
        _plain = _output.ToString();
        _output.GetStringBuilder().Clear();
        await Execute("quiet");
        _quiet = _output.ToString();
    }

    [Fact] void should_omit_the_property_in_json() => _json.ShouldNotContain("runningStateChangedAt");
    [Fact] void should_write_dash_in_plain() => _plain.TrimEnd().EndsWith("\t-").ShouldBeTrue();
    [Fact] void should_keep_quiet_output_to_the_identifier() => _quiet.Trim().ShouldEqual("obs-1");
}
