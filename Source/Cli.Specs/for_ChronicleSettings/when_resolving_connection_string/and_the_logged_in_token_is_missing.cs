// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_the_logged_in_token_is_missing : given.a_temp_config_directory
{
    Exception? _error;

    void Establish()
    {
        new CliConfiguration
        {
            ActiveContext = "production",
            Contexts = new Dictionary<string, CliContext>
            {
                ["production"] = new() { Server = "chronicle://production:35000", LoggedInUser = "admin", TokenServer = "production:35000" }
            }
        }.Save();
    }

    void Because() => _error = Catch.Exception(() => new ChronicleSettings().ResolveConnectionString());

    [Fact] void should_refuse_to_fall_back_to_the_development_client() => _error.ShouldBeOfExactType<LoginSessionExpired>();
    [Fact] void should_identify_the_missing_token() => _error!.Message.ShouldContain("no valid token");
}
