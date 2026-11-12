// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Cli.given;
using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required.given;

[Collection(CliSpecsCollection.Name)]
public class a_server_refusing_an_administrative_command : a_temp_config_directory
{
    protected int _result;
    protected StringWriter _error;
    TextWriter _previousError;

    void Establish()
    {
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    void Destroy() => Console.SetError(_previousError);

    protected static void StoreALogin() =>
        new CliConfiguration
        {
            ActiveContext = "production",
            Contexts = new Dictionary<string, CliContext>
            {
                ["production"] = new()
                {
                    LoggedInUser = "admin",
                    AccessToken = "stored-token",
                    TokenExpiry = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
                    TokenServer = "override:35001"
                }
            }
        }.Save();

    protected async Task Execute(RpcException failure, bool requiresAdministrator, string server = "chronicle://override:35001")
    {
        var command = new RefusingCommand(failure, requiresAdministrator);
        _result = await ((ICommand<ChronicleSettings>)command).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "test", null),
            new ChronicleSettings { Server = server, Output = OutputFormats.JsonCompact },
            CancellationToken.None);
    }

    public sealed class RefusingCommand(RpcException failure, bool requiresAdministrator) : ChronicleCommand<ChronicleSettings>
    {
        /// <inheritdoc/>
        protected override bool RequiresAdministrator => requiresAdministrator;

        /// <inheritdoc/>
        protected override Task<int> ExecuteCommandAsync(IServices services, ChronicleSettings settings, string format) => throw failure;
    }
}
