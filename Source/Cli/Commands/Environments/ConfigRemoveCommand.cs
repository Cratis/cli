// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Removes a configuration value.
/// </summary>
[LlmDescription("Removes a key from the selected environment. A secret kept in a vault is deleted from the vault too.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("rm", "Remove a configuration value or secret", Branch = typeof(ConfigBranch))]
[CliExample("config", "rm", "smtp:password")]
[LlmOption("<KEY>", "string", "Configuration key (positional)")]
public class ConfigRemoveCommand : AsyncCommand<ConfigKeySettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, ConfigKeySettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, async () =>
        {
            var project = EnvironmentCommandSupport.Project();
            var environment = EnvironmentCommandSupport.Open(settings, project);
            if (!await environment.RemoveAsync(settings.Key, cancellationToken))
            {
                OutputFormatter.WriteError(format, $"'{settings.Key}' is not set in environment '{environment.Name}'.", null, ExitCodes.NotFoundCode);
                return ExitCodes.NotFound;
            }

            OutputFormatter.WriteObject(
                format,
                new { environment = environment.Name, removed = settings.Key },
                _ => OutputFormatter.WriteMessage(format, $"Removed '{settings.Key}' from '{environment.Name}'"));
            return ExitCodes.Success;
        });
    }
}
