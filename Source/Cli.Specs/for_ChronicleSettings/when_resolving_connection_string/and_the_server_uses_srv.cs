// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.given;

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_the_server_uses_srv : a_temp_config_directory
{
    string _connection = null!;

    void Establish()
    {
        new CliConfiguration
        {
            ActiveContext = "production",
            Contexts = new Dictionary<string, CliContext>
            {
                ["production"] = new()
                {
                    Server = "chronicle://production:35000",
                    LoggedInUser = "admin",
                    AccessToken = "private-token",
                    TokenExpiry = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
                    TokenServer = "production:35000"
                }
            }
        }.Save();
    }

    void Because() => _connection = new ChronicleSettings { Server = "chronicle+srv://production:35000" }.ResolveConnectionString();

    [Fact] void should_not_attach_the_token_even_when_the_srv_name_matches_the_issuer() => _connection.ShouldNotContain("private-token");
    [Fact] void should_retain_the_srv_scheme() => _connection.ShouldContain("chronicle+srv://production:35000");
}
