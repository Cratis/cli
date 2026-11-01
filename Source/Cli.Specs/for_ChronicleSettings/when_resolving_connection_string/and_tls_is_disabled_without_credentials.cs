// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_tls_is_disabled_without_credentials : given.a_temp_config_directory
{
    string _result = string.Empty;

    void Establish() => new CliConfiguration
    {
        Contexts = new Dictionary<string, CliContext> { ["default"] = new() }
    }.Save();

    void Because() => _result = new ChronicleSettings { Server = "chronicle://localhost:35001/?disableTls=true" }.ResolveConnectionString();

    [Fact] void should_not_add_the_development_credentials() => _result.ShouldNotContain(ChronicleConnectionString.DevelopmentClient);
}
