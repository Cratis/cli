// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Auth;
using Cratis.Cli.for_ChronicleTokenCache.given;

namespace Cratis.Cli.for_LogoutCommand.given;

public class a_logout_command : a_token_cache
{
    protected int _result;
    protected bool _cleared;

    void Establish() => new CliConfiguration
    {
        ActiveContext = Context,
        Contexts = new Dictionary<string, CliContext>
        {
            [Context] = new()
            {
                ClientId = "client",
                ClientSecret = "fixture-secret",
                AccessToken = "user-fixture",
                TokenExpiry = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
                TokenServer = "chronicle://localhost:35000",
                LoggedInUser = "user"
            },
            ["other"] = new() { ClientId = "other", ClientSecret = "other-fixture" }
        }
    }.Save();

    protected async Task Execute()
    {
        _result = await new LogoutCommand().ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "logout", null),
            new GlobalSettings { Output = OutputFormats.JsonCompact },
            CancellationToken.None);
        var context = CliConfiguration.Load().GetCurrentContext();
        _cleared = context.ClientId is null && context.ClientSecret is null && context.AccessToken is null &&
            context.TokenExpiry is null && context.TokenServer is null && context.LoggedInUser is null;
    }
}
