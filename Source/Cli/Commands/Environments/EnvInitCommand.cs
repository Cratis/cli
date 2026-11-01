// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Creates an environment.
/// </summary>
[LlmDescription("Creates .cratis/environments/<name>.yaml (default name 'development') with a secrets provider: passphrase (default; AES-256-GCM, passphrase from CRATIS_PASSPHRASE, CRATIS_PASSPHRASE_FILE or a hidden prompt), azurekeyvault://<vault> or hashivault://<address>/<mount>[/<path>]. The passphrase is never written anywhere.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("init", "Create an environment", Branch = typeof(EnvBranch))]
[CliExample("env", "init")]
[CliExample("env", "init", "production")]
[CliExample("env", "init", "production", "--secrets-provider", "azurekeyvault://my-vault")]
[CliExample("env", "init", "production", "--secrets-provider", "hashivault://vault.example.com:8200/secret/cratis")]
[LlmOption("[NAME]", "string", "Environment name (positional). Defaults to 'development'.")]
[LlmOption("--secrets-provider", "string", "Where secrets are kept: passphrase (default), azurekeyvault://<vault> or hashivault://<address>/<mount>[/<path>].")]
public class EnvInitCommand : AsyncCommand<EnvInitSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, EnvInitSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, () =>
        {
            var project = EnvironmentCommandSupport.Project();
            var name = settings.Name ?? EnvironmentName.Default;
            var environment = ProjectEnvironment.Create(project, name, settings.SecretsProvider, EnvironmentCommandSupport.Passphrase(creating: true));
            project.EnsureLocalIgnored();
            var path = project.PathFor(name);

            OutputFormatter.WriteObject(
                format,
                new { environment = name, secretsProvider = environment.Provider.Text, path },
                _ => OutputFormatter.WriteMessage(format, $"Created environment '{name}' with the {environment.Provider.Text} secrets provider ({path})"));
            return Task.FromResult(ExitCodes.Success);
        });
    }
}
