// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Auth;

/// <summary>
/// Clears any stored login session (user or client credentials) from the active context.
/// </summary>
public class LogoutCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        if (string.IsNullOrWhiteSpace(ctx.ClientId) && string.IsNullOrWhiteSpace(ctx.AccessToken) && string.IsNullOrWhiteSpace(ctx.LoggedInUser))
        {
            OutputFormatter.WriteMessage(format, "No active login session.");
            return Task.FromResult(ExitCodes.Success);
        }

        var clientId = ctx.ClientId ?? ctx.LoggedInUser ?? "unknown";
        ctx.ClientId = null;
        ctx.ClientSecret = null;
        ctx.AccessToken = null;
        ctx.TokenExpiry = null;
        ctx.LoggedInUser = null;
        config.Save();

        if (!string.IsNullOrWhiteSpace(clientId) && clientId != "unknown")
        {
            var cachePath = CliConfiguration.GetTokenCachePath($"{config.ActiveContextName}_{clientId}");
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
        }

        OutputFormatter.WriteMessage(format, $"Logged out '{clientId}'. Credentials cleared.");
        return Task.FromResult(ExitCodes.Success);
    }
}
