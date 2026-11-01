// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Changes the passphrase of an environment.
/// </summary>
[LlmDescription("Re-encrypts every secret of a passphrase-protected environment under a new passphrase and a new salt. The current passphrase comes from CRATIS_PASSPHRASE, CRATIS_PASSPHRASE_FILE or a hidden prompt; the new one from CRATIS_NEW_PASSPHRASE, CRATIS_NEW_PASSPHRASE_FILE or a hidden prompt.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("change-passphrase", "Change the passphrase of an environment", Branch = typeof(EnvBranch))]
[CliExample("env", "change-passphrase")]
[CliExample("env", "change-passphrase", "-e", "production")]
public class EnvChangePassphraseCommand : AsyncCommand<EnvironmentSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, EnvironmentSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, async () =>
        {
            var project = EnvironmentCommandSupport.Project();
            var name = settings.ResolveEnvironment(project).Name;
            var environment = ProjectEnvironment.Open(project, name, EnvironmentCommandSupport.Passphrase(creating: false));
            var moved = await environment.ChangePassphraseAsync(cancellationToken);
            OutputFormatter.WriteObject(
                format,
                new { environment = name, secretsReencrypted = moved },
                _ => OutputFormatter.WriteMessage(format, $"Changed the passphrase of '{name}' and re-encrypted {moved} secret(s)"));
            return ExitCodes.Success;
        });
    }
}
