// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Workbench;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ChronicleSettings.when_resolving_connection_string;

[Collection(CliSpecsCollection.Name)]
public class and_the_workbench_uses_another_server : a_temp_config_directory
{
    string _connection = null!;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        ChronicleSettings.ResetWarningsForSpecs();
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
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    void Because()
    {
        var settings = new WorkbenchSettings { Server = "chronicle://other:35000", Debug = true };
        _connection = settings.ResolveConnectionString();
        settings.ResolveConnectionString();
    }

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            Console.SetError(_previousError);
            ChronicleSettings.ResetWarningsForSpecs();
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Fact] void should_not_send_the_token() => _connection.ShouldNotContain("private-token");
    [Fact] void should_not_write_debug_messages_over_the_tui() => _error.ToString().ShouldBeEmpty();
}
