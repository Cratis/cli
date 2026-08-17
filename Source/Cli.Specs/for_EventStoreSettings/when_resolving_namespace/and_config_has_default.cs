// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_EventStoreSettings.when_resolving_namespace;

[Collection(CliSpecsCollection.Name)]
public class and_config_has_default : given.a_temp_config_directory
{
    const string ExpectedNamespace = "configured-namespace";

    EventStoreSettings _settings;
    ResolvedSetting _result;

    void Establish()
    {
        var config = new CliConfiguration
        {
            ActiveContext = "default",
            Contexts = new Dictionary<string, CliContext>
            {
                ["default"] = new CliContext { Namespace = ExpectedNamespace }
            }
        };
        config.Save();
        _settings = new EventStoreSettings();
    }

    void Because() => _result = _settings.ResolveNamespaceWithSource();

    [Fact] void should_return_the_config_value() => _result.Value.ShouldEqual(ExpectedNamespace);
    [Fact] void should_come_from_the_context() => _result.Source.ShouldEqual(SettingSource.Context);
}
