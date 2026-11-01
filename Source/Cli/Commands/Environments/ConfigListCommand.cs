// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Lists the configuration of the selected environment.
/// </summary>
[LlmDescription("Lists the keys of the selected environment; secrets show as [secret] and are never resolved. With --missing <path> it lists the keys the model at that path needs that the environment lacks, and exits with the validation code (5) when any are missing.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("list", "List configuration values (secrets shown as [secret])", Branch = typeof(ConfigBranch), IsDefault = true)]
[CliExample("config")]
[CliExample("config", "--missing", "keys.txt")]
[CliExample("config", "-e", "production", "-o", "json")]
[LlmOption("--missing", "string", "Path of a model key list; prints the keys the environment is missing and exits with 5 when there are any")]
public class ConfigListCommand : AsyncCommand<ConfigListSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, ConfigListSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, () =>
        {
            var project = EnvironmentCommandSupport.Project();
            var name = settings.ResolveEnvironment(project).Name;
            var document = project.Load(name);

            return Task.FromResult(settings.Missing is null ? List(format, name, document) : ReportMissing(format, name, document, settings.Missing));
        });
    }

    static int List(string format, string name, EnvironmentDocument document)
    {
        var entries = document.Config.Select(pair => new ConfigListEntry(pair.Key, pair.Value.IsSecret ? ConfigGetCommand.SecretPlaceholder : pair.Value.Value, pair.Value.IsSecret)).ToArray();
        if (entries.Length == 0)
        {
            OutputFormatter.WriteMessage(format, $"Environment '{name}' has no configuration. Add some with: cratis config set <key> <value>");
            return ExitCodes.Success;
        }

        OutputFormatter.Write(format, entries, ["Key", "Value"], entry => [entry.Key, entry.Value], entry => entry.Key);
        return ExitCodes.Success;
    }

    static int ReportMissing(string format, string name, EnvironmentDocument document, string path)
    {
        var missing = ModelKeySources.Read(path).Where(key => !document.Config.ContainsKey(key)).ToArray();
        if (missing.Length == 0)
        {
            OutputFormatter.WriteMessage(format, $"Environment '{name}' has every key the model needs");
            return ExitCodes.Success;
        }

        OutputFormatter.Write(format, missing.Select(key => new MissingKey(key)).ToArray(), ["Missing key"], entry => [entry.Key], entry => entry.Key);
        return ExitCodes.ValidationError;
    }

    sealed record ConfigListEntry(string Key, string Value, bool Secret);

    sealed record MissingKey(string Key);
}
