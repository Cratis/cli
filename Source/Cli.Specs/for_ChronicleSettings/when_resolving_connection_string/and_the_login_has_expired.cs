// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_the_login_has_expired : given.a_temp_config_directory
{
    Exception? _error;

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
                    LoggedInUser = "admin",
                    ClientId = "old-client",
                    ClientSecret = "old-secret"
                }
            }
        }.Save();
    }

    void Because() => _error = Catch.Exception(() => new ChronicleSettings { Server = "chronicle://override:35001" }.ResolveConnectionString());

    [Fact] void should_refuse_to_use_the_development_client_or_old_credentials() => _error.ShouldBeOfExactType<LoginSessionExpired>();
    [Fact] void should_tell_the_user_to_log_in_again() => _error!.Message.ShouldContain("cratis chronicle login");
}
