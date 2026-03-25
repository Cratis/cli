// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Config;

/// <summary>
/// Sets a configuration value.
/// </summary>
[CliCommand("set", "Set a configuration value", Branch = typeof(ConfigBranch))]
[CliExample("config", "set", "server", "chronicle://myhost:35000")]
[CliExample("config", "set", "client-id", "my-app")]
[LlmOption("<KEY>", "string", "Key to set: server, event-store, namespace, client-id, client-secret, management-port (positional). These update the active context's defaults.")]
[LlmOption("<VALUE>", "string", "Value to assign (positional)")]
public class SetConfigCommand : AsyncCommand<SetConfigSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, SetConfigSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        switch (settings.Key.ToLowerInvariant())
        {
            case "server":
                ctx.Server = settings.Value;
                break;
            case "event-store":
                ctx.EventStore = settings.Value;
                break;
            case "namespace":
                ctx.Namespace = settings.Value;
                break;
            case "client-id":
                ctx.ClientId = settings.Value;
                break;
            case "client-secret":
                ctx.ClientSecret = settings.Value;
                break;
            case "management-port":
                if (int.TryParse(settings.Value, out var port))
                {
                    ctx.ManagementPort = port;
                }
                else
                {
                    OutputFormatter.WriteError(format, $"Invalid port value: '{settings.Value}'", "management-port must be a valid integer.", ExitCodes.ValidationErrorCode);
                    return Task.FromResult(ExitCodes.NotFound);
                }
                break;
            default:
                OutputFormatter.WriteError(format, $"Unknown config key: '{settings.Key}'", "Valid keys: server, event-store, namespace, client-id, client-secret, management-port", ExitCodes.NotFoundCode);
                return Task.FromResult(ExitCodes.NotFound);
        }

        config.Save();
        OutputFormatter.WriteMessage(format, $"Set '{settings.Key}' to '{settings.Value}' in context '{config.ActiveContextName}'");
        return Task.FromResult(ExitCodes.Success);
    }
}
