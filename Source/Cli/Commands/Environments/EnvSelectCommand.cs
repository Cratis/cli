// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Selects the environment this user works with.
/// </summary>
[LlmDescription("Selects the environment for this user by writing .cratis/local/selected-environment, which is not committed. --environment/-e and CRATIS_ENVIRONMENT override it per command.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("select", "Select the environment to use", Branch = typeof(EnvBranch))]
[CliExample("env", "select", "production")]
[LlmOption("<NAME>", "string", "Environment to select (positional); must exist")]
public class EnvSelectCommand : AsyncCommand<EnvNameSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, EnvNameSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, () =>
        {
            var project = EnvironmentCommandSupport.Project();
            if (!project.Exists(settings.Name))
            {
                throw new EnvironmentNotFoundException(settings.Name);
            }

            project.Select(settings.Name);
            OutputFormatter.WriteObject(
                format,
                new { selected = settings.Name },
                _ => OutputFormatter.WriteMessage(format, $"Selected environment '{settings.Name}'"));
            return Task.FromResult(ExitCodes.Success);
        });
    }
}
