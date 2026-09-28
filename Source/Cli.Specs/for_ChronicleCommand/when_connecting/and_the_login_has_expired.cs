// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ChronicleCommand.when_connecting;

[Collection(CliSpecsCollection.Name)]
public class and_the_login_has_expired : a_temp_config_directory
{
    int _result;
    bool _commandExecuted;

    void Establish()
    {
        new CliConfiguration
        {
            ActiveContext = "production",
            Contexts = new Dictionary<string, CliContext>
            {
                ["production"] = new()
                {
                    LoggedInUser = "admin",
                    AccessToken = "expired-token",
                    TokenExpiry = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O")
                }
            }
        }.Save();
    }

    async Task Because()
    {
        var command = new TestChronicleCommand(() => _commandExecuted = true);
        _result = await ((ICommand<ChronicleSettings>)command).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "test", null),
            new ChronicleSettings { Server = "chronicle://override:35001", Output = OutputFormats.JsonCompact, Debug = true },
            CancellationToken.None);
    }

    [Fact] void should_return_an_authentication_error() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_execute_the_command() => _commandExecuted.ShouldBeFalse();

    /// <summary>
    /// A command used to assert that connection resolution fails before contacting the server.
    /// </summary>
    /// <param name="executed">Callback invoked when execution unexpectedly reaches the server.</param>
    public sealed class TestChronicleCommand(Action executed) : ChronicleCommand<ChronicleSettings>
    {
        /// <inheritdoc/>
        protected override Task<int> ExecuteCommandAsync(IServices services, ChronicleSettings settings, string format)
        {
            executed();
            return Task.FromResult(ExitCodes.Success);
        }
    }
}
