// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_EventStoreSettings.when_resolving_namespace;

[Collection(CliSpecsCollection.Name)]
public class and_no_override_is_configured : given.a_temp_config_directory
{
    EventStoreSettings _settings;
    ResolvedSetting _result;

    void Establish() => _settings = new EventStoreSettings();

    void Because() => _result = _settings.ResolveNamespaceWithSource();

    [Fact] void should_return_default() => _result.Value.ShouldEqual(CliDefaults.DefaultNamespaceName);
    [Fact] void should_come_from_the_built_in_default() => _result.Source.ShouldEqual(SettingSource.Default);
}
