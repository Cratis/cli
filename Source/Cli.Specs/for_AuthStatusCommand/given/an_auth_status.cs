// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Chronicle.Auth;
using Cratis.Cli.given;

namespace Cratis.Cli.for_AuthStatusCommand.given;

public class an_auth_status : a_temp_config_directory
{
    protected ChronicleSettings _settings = null!;
    protected CliConfiguration _configuration = null!;
    protected JsonDocument _status = null!;
    protected int _result;
    TextWriter _previousOutput = null!;
    StringWriter _output = null!;
    string? _previousEnvironment;

    void Establish()
    {
        _configuration = new CliConfiguration
        {
            Contexts = new Dictionary<string, CliContext>
            {
                ["default"] = new() { Server = "chronicle://production:35000", LoggedInUser = "admin", TokenServer = "production:35000", AccessToken = "private-token" }
            }
        };
        _settings = new ChronicleSettings { Output = OutputFormats.JsonCompact };
        _previousOutput = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
        _previousEnvironment = Environment.GetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar);
        Environment.SetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar, null);
    }

    protected async Task Execute()
    {
        _configuration.Save();
        _result = await ((ICommand<ChronicleSettings>)new AuthStatusCommand()).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "status", null), _settings, CancellationToken.None);
        _status = JsonDocument.Parse(_output.ToString());
    }

    protected override void CleanUp()
    {
        Console.SetOut(_previousOutput);
        Environment.SetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar, _previousEnvironment);
        _status?.Dispose();
        _output.Dispose();
        base.CleanUp();
    }
}
