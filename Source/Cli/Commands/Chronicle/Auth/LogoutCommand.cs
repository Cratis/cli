// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Auth;

/// <summary>
/// Clears any stored login session (user or client credentials) from the active context.
/// </summary>
[LlmDescription("Clears the cached authentication token. Use when switching users or after a session expires.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("logout", "Clear the cached login session", Branch = typeof(ChronicleBranch))]
[CliExample("chronicle", "logout")]
[LlmOutputAdvice("plain", "Top-level command (not 'auth logout'). Plain outputs a simple confirmation message.")]
public class LogoutCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        if (!HasSession(ctx))
        {
            OutputFormatter.WriteMessage(format, "No active login session.");
            return Task.FromResult(ExitCodes.Success);
        }

        var cacheClientId = ctx.ClientId ?? ctx.LoggedInUser;
        var clientId = cacheClientId ?? "unknown";
        ctx.ClientId = null;
        ctx.ClientSecret = null;
        ctx.AccessToken = null;
        ctx.TokenExpiry = null;
        ctx.TokenServer = null;
        ctx.LoggedInUser = null;
        config.Save();

        if (!string.IsNullOrWhiteSpace(cacheClientId))
        {
            ChronicleTokenCache.ClearIdentity(config.ActiveContextName, cacheClientId);
        }

        OutputFormatter.WriteMessage(format, $"Logged out '{clientId}'. Credentials cleared.");
        return Task.FromResult(ExitCodes.Success);
    }

    static bool HasSession(CliContext context) =>
        !string.IsNullOrWhiteSpace(context.ClientId) || !string.IsNullOrWhiteSpace(context.AccessToken) || !string.IsNullOrWhiteSpace(context.LoggedInUser);
}
