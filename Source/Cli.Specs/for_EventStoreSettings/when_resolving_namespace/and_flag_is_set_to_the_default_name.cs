// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_EventStoreSettings.when_resolving_namespace;

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
                ["default"] = new CliContext { Namespace = "configured-namespace" }
            }
        };
        config.Save();
        _settings = new EventStoreSettings { Namespace = CliDefaults.DefaultNamespaceName };
    }

    void Because() => _result = _settings.ResolveNamespaceWithSource();

    [Fact] void should_return_the_default_name_and_not_the_context_namespace() => _result.Value.ShouldEqual(CliDefaults.DefaultNamespaceName);
    [Fact] void should_come_from_the_option() => _result.Source.ShouldEqual(SettingSource.Option);
}
