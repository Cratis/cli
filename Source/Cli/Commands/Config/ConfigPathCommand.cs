// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Config;

/// <summary>
/// Prints the configuration file path.
/// </summary>
[CliCommand("path", "Print configuration file path", Branch = typeof(ConfigBranch))]
[CliExample("config", "path")]
[LlmOutputAdvice("plain", "Both formats output identical raw path text.")]
public class ConfigPathCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        Console.WriteLine(CliConfiguration.GetConfigPath());
        return Task.FromResult(ExitCodes.Success);
    }
}
