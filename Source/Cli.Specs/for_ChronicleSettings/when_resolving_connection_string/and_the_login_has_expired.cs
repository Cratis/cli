// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_the_login_has_expired : given.a_temp_config_directory
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
                    AccessToken = "expired-token",
                    TokenExpiry = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"),
                    TokenServer = "production:35000",
                    LoggedInUser = "admin",
                    ClientId = "old-client",
                    ClientSecret = "old-secret"
                }
            }
        }.Save();
    }

    void Because() => _connection = new ChronicleSettings().ResolveConnectionString();

    [Fact] void should_use_the_explicit_client_credentials() => _connection.ShouldContain("old-client:old-secret@production");
    [Fact] void should_not_use_the_expired_token() => _connection.ShouldNotContain("expired-token");
}
