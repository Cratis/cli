// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Context;

/// <summary>
/// Sets the current context to the specified named context.
/// </summary>
[CliCommand("set", "Switch to a context", Branch = typeof(ContextBranch), DynamicCompletion = "contexts")]
[CliExample("context", "set", "prod")]
[LlmOutputAdvice("plain", "Plain outputs a confirmation message.")]
[LlmOption("<NAME>", "string", "Name of the context to switch to (positional)")]
public class SetContextCommand : AsyncCommand<ContextNameSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, ContextNameSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        if (!config.Contexts.ContainsKey(settings.Name))
        {
            OutputFormatter.WriteError(format, $"Context '{settings.Name}' does not exist", $"Available contexts: {string.Join(", ", config.Contexts.Keys)}", ExitCodes.NotFoundCode);
            return Task.FromResult(ExitCodes.NotFound);
        }

        config.ActiveContext = settings.Name;
        config.Save();

        OutputFormatter.WriteMessage(format, $"Switched to context '{settings.Name}'.");
        return Task.FromResult(ExitCodes.Success);
    }
}
