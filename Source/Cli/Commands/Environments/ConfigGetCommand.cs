// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Gets a configuration value.
/// </summary>
[LlmDescription("Prints a key of the selected environment. A secret prints as [secret] unless --show-secret is passed, which resolves it through the secrets provider.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("get", "Get a configuration value", Branch = typeof(ConfigBranch))]
[CliExample("config", "get", "auth:clientId")]
[CliExample("config", "get", "smtp:password", "--show-secret")]
[LlmOption("<KEY>", "string", "Configuration key (positional)")]
[LlmOption("--show-secret", "bool", "Reveal a secret. Without it a secret is printed as [secret].")]
public class ConfigGetCommand : AsyncCommand<ConfigGetSettings>
{
    /// <summary>
    /// The text shown in place of a secret value.
    /// </summary>
    public const string SecretPlaceholder = "[secret]";

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, ConfigGetSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        return EnvironmentCommandSupport.Run(format, async () =>
        {
            var project = EnvironmentCommandSupport.Project();
            var environment = EnvironmentCommandSupport.Open(settings, project);
            var entry = environment.GetEntry(settings.Key);
            if (entry is null)
            {
                OutputFormatter.WriteError(format, $"'{settings.Key}' is not set in environment '{environment.Name}'.", null, ExitCodes.NotFoundCode);
                return ExitCodes.NotFound;
            }

            var value = entry.Value;
            if (entry.IsSecret)
            {
                value = settings.ShowSecret ? await environment.GetValueAsync(settings.Key, cancellationToken) ?? string.Empty : SecretPlaceholder;
            }

            if (string.Equals(format, OutputFormats.Table, StringComparison.Ordinal) || string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal) || string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal))
            {
                Console.WriteLine(value);
                return ExitCodes.Success;
            }

            OutputFormatter.WriteObject(format, new { environment = environment.Name, key = settings.Key, value, secret = entry.IsSecret });
            return ExitCodes.Success;
        });
    }
}
