// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Context;

/// <summary>
/// Renames an existing context.
/// </summary>
[CliCommand("rename", "Rename a context", Branch = typeof(ContextBranch), DynamicCompletion = "contexts")]
[CliExample("context", "rename", "dev", "development")]
[LlmOutputAdvice("plain", "Plain outputs a confirmation message.")]
[LlmOption("<OLD_NAME>", "string", "Current context name (positional)")]
[LlmOption("<NEW_NAME>", "string", "New context name (positional)")]
public class RenameContextCommand : AsyncCommand<RenameContextSettings>
{
    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, RenameContextSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        if (!config.Contexts.TryGetValue(settings.OldName, out var ctx))
        {
            OutputFormatter.WriteError(format, $"Context '{settings.OldName}' does not exist", errorCode: ExitCodes.NotFoundCode);
            return Task.FromResult(ExitCodes.NotFound);
        }

        if (config.Contexts.ContainsKey(settings.NewName))
        {
            OutputFormatter.WriteError(format, $"Context '{settings.NewName}' already exists", errorCode: ExitCodes.ValidationErrorCode);
            return Task.FromResult(ExitCodes.ValidationError);
        }

        config.Contexts.Remove(settings.OldName);
        config.Contexts[settings.NewName] = ctx;

        if (config.ActiveContext == settings.OldName)
        {
            config.ActiveContext = settings.NewName;
        }

        config.Save();

        OutputFormatter.WriteMessage(format, $"Renamed context '{settings.OldName}' to '{settings.NewName}'.");
        return Task.FromResult(ExitCodes.Success);
    }
}
