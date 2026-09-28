// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_a_legacy_login_has_no_token : given.a_temp_config_directory
{
    string _connection = null!;
    StringWriter _warning = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        new CliConfiguration
        {
            ActiveContext = "legacy",
            Contexts = new Dictionary<string, CliContext>
            {
                ["legacy"] = new() { Server = "chronicle://legacy:35000", LoggedInUser = "admin" }
            }
        }.Save();
        _previousError = Console.Error;
        _warning = new StringWriter();
        Console.SetError(_warning);
    }

    void Because()
    {
        _connection = new ChronicleSettings().ResolveConnectionString();
        Console.SetError(_previousError);
    }

    [Fact] void should_fall_back_to_development_credentials() => _connection.ShouldContain(ChronicleConnectionString.DevelopmentClient);
    [Fact] void should_warn_on_stderr_to_log_in_again() => _warning.ToString().ShouldContain("cratis chronicle login");
    [Fact] void should_not_include_warning_in_connection_string() => _connection.ShouldNotContain("Warning:");
}
