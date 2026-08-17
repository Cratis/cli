// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_EventStoreSettings.when_resolving_event_store;

[Collection(CliSpecsCollection.Name)]
public class and_flag_is_set_to_the_default_name : given.a_temp_config_directory
{
    EventStoreSettings _settings;
    ResolvedSetting _result;

    void Establish()
    {
        var config = new CliConfiguration
        {
            ActiveContext = "default",
            Contexts = new Dictionary<string, CliContext>
            {
                ["default"] = new CliContext { EventStore = "configured-store" }
            }
        };
        config.Save();
        _settings = new EventStoreSettings { EventStore = CliDefaults.DefaultEventStoreName };
    }

    void Because() => _result = _settings.ResolveEventStoreWithSource();

    [Fact] void should_return_the_default_name_and_not_the_context_event_store() => _result.Value.ShouldEqual(CliDefaults.DefaultEventStoreName);
    [Fact] void should_come_from_the_option() => _result.Source.ShouldEqual(SettingSource.Option);
}
