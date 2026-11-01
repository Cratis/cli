// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_no_authentication_is_requested : given.a_temp_config_directory
{
    string _result = string.Empty;

    void Establish() => new CliConfiguration
    {
        Contexts = new Dictionary<string, CliContext> { ["default"] = new() { ClientId = "service", ClientSecret = "secret" } }
    }.Save();

    void Because() => _result = new ChronicleSettings { Server = "chronicle://localhost:35001/?disableTls=true&auth=none" }.ResolveConnectionString();

    [Fact] void should_leave_the_connection_string_as_it_is() => _result.ShouldEqual("chronicle://localhost:35001/?disableTls=true&auth=none");
}
