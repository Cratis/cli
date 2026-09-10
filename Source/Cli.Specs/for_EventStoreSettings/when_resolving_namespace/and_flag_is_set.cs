// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_EventStoreSettings.when_resolving_namespace;

[Collection(CliSpecsCollection.Name)]
public class and_flag_is_set : given.a_temp_config_directory
{
    const string ExpectedNamespace = "my-namespace";

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
        _settings = new EventStoreSettings { Namespace = ExpectedNamespace };
    }

    void Because() => _result = _settings.ResolveNamespaceWithSource();

    [Fact] void should_return_the_flag_value() => _result.Value.ShouldEqual(ExpectedNamespace);
    [Fact] void should_come_from_the_option() => _result.Source.ShouldEqual(SettingSource.Option);
}
