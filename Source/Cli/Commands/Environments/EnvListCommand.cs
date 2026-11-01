// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Lists the environments of the project.
/// </summary>
[LlmDescription("Lists the environments in .cratis/environments and marks the one in effect, together with where that choice came from (option, CRATIS_ENVIRONMENT, selection file or default).")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("ls", "List environments", Branch = typeof(EnvBranch))]
[CliExample("env", "ls")]
[LlmOutputAdvice("json", "Each entry has name, selected, provider and the source of the selection.")]
public class EnvListCommand : AsyncCommand<EnvironmentSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, EnvironmentSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, () =>
        {
            var project = EnvironmentCommandSupport.Project();
            var selected = settings.ResolveEnvironment(project);
            var entries = project.List().Select(name => new EnvironmentEntry
            {
                Name = name,
                Selected = string.Equals(name, selected.Name, StringComparison.Ordinal),
                Provider = project.Load(name).SecretsProvider,
                Source = string.Equals(name, selected.Name, StringComparison.Ordinal) ? selected.Source.ToString() : string.Empty
            }).ToArray();

            if (entries.Length == 0)
            {
                OutputFormatter.WriteMessage(format, "No environments. Create one with: cratis env init [name]");
                return Task.FromResult(ExitCodes.Success);
            }

            OutputFormatter.Write(
                format,
                entries,
                ["Selected", "Name", "Secrets provider", "Selected by"],
                entry => [entry.Selected ? "*" : string.Empty, entry.Name, entry.Provider, entry.Source],
                entry => entry.Name);
            return Task.FromResult(ExitCodes.Success);
        });
    }

    sealed record EnvironmentEntry
    {
        public string Name { get; init; } = string.Empty;
        public bool Selected { get; init; }
        public string Provider { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
    }
}
