// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;
using Cratis.Environments.Secrets;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Removes an environment.
/// </summary>
[LlmDescription("Deletes .cratis/environments/<name>.yaml. Secrets kept in a vault are removed from the vault too. Prompts for confirmation unless --yes is passed.")]
[CommandEffect(CommandEffect.Destructive)]
[CliCommand("rm", "Remove an environment", Branch = typeof(EnvBranch))]
[CliExample("env", "rm", "staging")]
[CliExample("env", "rm", "staging", "--yes")]
[LlmOption("<NAME>", "string", "Environment to remove (positional)")]
public class EnvRemoveCommand : AsyncCommand<EnvNameSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, EnvNameSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, async () =>
        {
            var project = EnvironmentCommandSupport.Project();
            var document = project.Load(settings.Name);

            var exit = ConfirmationHelper.ConfirmOrExit(settings, $"Remove environment '{settings.Name}' and its configuration?", format);
            if (exit is not null)
            {
                return exit.Value;
            }

            var environment = ProjectEnvironment.Open(project, settings.Name, PassphraseSources.Fixed(string.Empty));
            foreach (var key in document.Config.Where(pair => pair.Value.Kind == ConfigEntryKind.Reference).Select(pair => pair.Key).ToArray())
            {
                await environment.RemoveAsync(key, cancellationToken: default);
            }

            project.Delete(settings.Name);
            OutputFormatter.WriteObject(
                format,
                new { removed = settings.Name },
                _ => OutputFormatter.WriteMessage(format, $"Removed environment '{settings.Name}'"));
            return ExitCodes.Success;
        });
    }
}
