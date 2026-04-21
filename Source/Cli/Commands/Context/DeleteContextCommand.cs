// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Context;

/// <summary>
/// Deletes a named context. Cannot delete the currently active context.
/// </summary>
[CliCommand("delete", "Delete a context", Branch = typeof(ContextBranch), DynamicCompletion = "contexts")]
[CliExample("context", "delete", "old-dev")]
[LlmOutputAdvice("plain", "Plain outputs a confirmation message.")]
[LlmOption("<NAME>", "string", "Name of the context to delete (positional)")]
public class DeleteContextCommand : AsyncCommand<ContextNameSettings>
{
    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, ContextNameSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        if (!config.Contexts.ContainsKey(settings.Name))
        {
            OutputFormatter.WriteError(format, $"Context '{settings.Name}' does not exist", errorCode: ExitCodes.NotFoundCode);
            return Task.FromResult(ExitCodes.NotFound);
        }

        if (config.ActiveContextName == settings.Name)
        {
            OutputFormatter.WriteError(format, $"Cannot delete the active context '{settings.Name}'", "Switch to a different context first with: cratis context set <other>", ExitCodes.ValidationErrorCode);
            return Task.FromResult(ExitCodes.ValidationError);
        }

        config.Contexts.Remove(settings.Name);
        config.Save();

        OutputFormatter.WriteMessage(format, $"Deleted context '{settings.Name}'.");
        return Task.FromResult(ExitCodes.Success);
    }
}
