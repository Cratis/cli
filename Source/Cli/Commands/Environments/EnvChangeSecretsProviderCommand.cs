// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Moves every secret of an environment to another secrets provider.
/// </summary>
[LlmDescription("Re-encrypts or moves every secret of the environment to another secrets provider (passphrase, azurekeyvault://<vault>, hashivault://<address>/<mount>[/<path>]) and removes previous material only when safe; uncertain store identities are kept with a warning. The current secrets are read with the current provider first; no clear text is written to the file. A new passphrase comes from CRATIS_NEW_PASSPHRASE, CRATIS_NEW_PASSPHRASE_FILE or a hidden prompt.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("change-secrets-provider", "Move every secret to another secrets provider", Branch = typeof(EnvBranch))]
[CliExample("env", "change-secrets-provider", "azurekeyvault://my-vault")]
[CliExample("env", "change-secrets-provider", "passphrase", "-e", "production")]
[LlmOption("<PROVIDER>", "string", "Target provider: passphrase, azurekeyvault://<vault> or hashivault://<address>/<mount>[/<path>] (positional)")]
public class EnvChangeSecretsProviderCommand : AsyncCommand<EnvChangeSecretsProviderSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, EnvChangeSecretsProviderSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, async () =>
        {
            var project = EnvironmentCommandSupport.Project();
            var name = settings.ResolveEnvironment(project).Name;
            var environment = ProjectEnvironment.Open(project, name, EnvironmentCommandSupport.Passphrase(creating: false));
            var moved = await environment.ChangeSecretsProviderAsync(settings.Provider, cancellationToken);
            await WriteMoveResult(format, environment, moved);
            return ExitCodes.Success;
        });
    }

    internal static async Task WriteMoveResult(string format, ProjectEnvironment environment, int moved)
    {
        foreach (var warning in environment.SecretRemovalWarnings)
        {
            await Console.Error.WriteLineAsync($"Warning: {warning}");
        }

        OutputFormatter.WriteObject(
            format,
            new { environment = environment.Name, secretsProvider = environment.Provider.Text, secretsMoved = moved, secretsRemoved = environment.SecretsRemoved, warnings = environment.SecretRemovalWarnings },
            _ => OutputFormatter.WriteMessage(format, $"Moved {moved} secret(s) of '{environment.Name}' to the {environment.Provider.Text} provider"));
    }
}
