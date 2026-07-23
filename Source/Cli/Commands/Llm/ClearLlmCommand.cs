// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Llm;

/// <summary>
/// Removes the language model configuration.
/// </summary>
[LlmDescription("Removes the language model configuration, including the stored API key. Destructive — prompts for confirmation unless --yes is specified.")]
[CliCommand("clear", "Remove the configured language model", Branch = typeof(LlmBranch))]
[CliExample("llm", "clear")]
[CliExample("llm", "clear", "--yes")]
[LlmOutputAdvice("plain", "Plain outputs a confirmation message.")]
public class ClearLlmCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        if (config.Llm is null)
        {
            OutputFormatter.WriteMessage(format, "No language model configured.");
            return Task.FromResult(ExitCodes.Success);
        }

        if (!ConfirmationHelper.ShouldProceed(settings, "Are you sure you want to remove the language model configuration?"))
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return Task.FromResult(ExitCodes.Success);
        }

        config.Llm = null;
        config.Save();

        OutputFormatter.WriteMessage(format, "Removed the language model configuration.");
        return Task.FromResult(ExitCodes.Success);
    }
}
