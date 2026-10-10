// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_the_token_belongs_to_an_equivalent_ipv6_address : given.a_temp_config_directory
{
    string _connection = null!;

    void Establish() => new CliConfiguration
    {
        Contexts = new Dictionary<string, CliContext>
        {
            ["default"] = new() { LoggedInUser = "admin", AccessToken = "ipv6-token", TokenServer = "[2001:db8::1]:35000", TokenExpiry = DateTimeOffset.UtcNow.AddHours(1).ToString("O") }
        }
    }.Save();

    void Because() => _connection = new ChronicleSettings { Server = "chronicle://[2001:DB8:0:0:0:0:0:1]:35000" }.ResolveConnectionString();

    [Fact] void should_use_the_bound_token() => _connection.ShouldContain("apiKey=ipv6-token");
}
