// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Sets a configuration value or secret.
/// </summary>
[LlmDescription("Sets a key in the selected environment. A plain value is the second argument. A key under secrets. (a named secret of the model) is always a secret, with or without --secret. With --secret the value is read from stdin (pipe it in) or a hidden prompt and never from an argument; it is stored encrypted or in the environment's vault and never echoed.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("set", "Set a configuration value or secret", Branch = typeof(ConfigBranch))]
[CliExample("config", "set", "auth:clientId", "my-client")]
[CliExample("config", "set", "smtp:password", "--secret")]
[CliExample("config", "set", "secrets.StripeKey")]
[CliExample("config", "set", "systems.Payments.accountId", "acct_123")]
[CliExample("config", "set", "smtp:password", "--secret", "-e", "production")]
[LlmOption("<KEY>", "string", "Configuration key (positional)")]
[LlmOption("[VALUE]", "string", "Plain value (positional). Not accepted with --secret.")]
[LlmOption("--secret", "bool", "Read the value from stdin or a hidden prompt and protect it with the secrets provider")]
public class ConfigSetCommand : AsyncCommand<ConfigSetSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, ConfigSetSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, async () =>
        {
            string value;
            var secret = settings.Secret || settings.Key.StartsWith(EnvironmentRequirements.SecretPrefix, StringComparison.Ordinal);
            if (secret)
            {
                if (settings.Value is not null)
                {
                    OutputFormatter.WriteError(format, "A secret is never read from a command-line argument, which would end up in shell history.", "Pipe the value in (echo -n \"$VALUE\" | cratis config set KEY --secret) or run interactively to be prompted.", ExitCodes.ValidationErrorCode);
                    return ExitCodes.ValidationError;
                }

                value = SecretInput.Read();
            }
            else
            {
                if (settings.Value is null)
                {
                    OutputFormatter.WriteError(format, $"No value given for '{settings.Key}'.", "Pass a value, or use --secret to be prompted.", ExitCodes.ValidationErrorCode);
                    return ExitCodes.ValidationError;
                }

                value = settings.Value;
            }

            var project = EnvironmentCommandSupport.Project();
            var environment = EnvironmentCommandSupport.Open(settings, project);
            await environment.SetAsync(settings.Key, value, secret, cancellationToken);
            OutputFormatter.WriteObject(
                format,
                new { environment = environment.Name, key = settings.Key, secret },
                _ => OutputFormatter.WriteMessage(format, secret ? $"Stored secret '{settings.Key}' in '{environment.Name}'" : $"Set '{settings.Key}' in '{environment.Name}'"));
            return ExitCodes.Success;
        });
    }
}
