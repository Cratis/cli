// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_the_server_differs_from_the_token_issuer : given.a_temp_config_directory
{
    string _override = null!;
    string _environment = null!;
    string? _previousEnvironment;
    StringWriter _debug = null!;
    TextWriter _previousError = null!;

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
        _previousEnvironment = Environment.GetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar);
        _previousError = Console.Error;
        _debug = new StringWriter();
        Console.SetError(_debug);
    }

    void Because()
    {
        _override = new ChronicleSettings { Server = "chronicle://other:35001", Debug = true }.ResolveConnectionString();
        Environment.SetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar, "chronicle://environment:35002");
        _environment = new ChronicleSettings().ResolveConnectionString();
        Environment.SetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar, _previousEnvironment);
        Console.SetError(_previousError);
    }

    [Fact] void should_not_send_the_token_to_the_override() => _override.ShouldNotContain("private-token");
    [Fact] void should_not_send_the_token_to_the_environment_server() => _environment.ShouldNotContain("private-token");
    [Fact] void should_use_the_development_client_for_the_override() => _override.ShouldContain(ChronicleConnectionString.DevelopmentClient);
    [Fact] void should_explain_the_mismatch_only_in_debug() => _debug.ToString().ShouldContain("belongs to production:35000");
}
