// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chat;

/// <summary>
/// Handles confirmation prompts for write operations invoked by the AI chat.
/// </summary>
public static class ToolCallConfirmationHandler
{
    /// <summary>
    /// Checks if the given tool name requires user confirmation.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <returns>True if the tool is a write operation.</returns>
    public static bool RequiresConfirmation(string toolName) =>
        ChronicleChatTools.WriteToolNames.Contains(toolName);

    /// <summary>
    /// Prompts the user for confirmation before executing a write tool.
    /// </summary>
    /// <param name="toolName">The tool being invoked.</param>
    /// <param name="autoConfirm">If true, skip the prompt (e.g. when --yes is set).</param>
    /// <returns>True if the user confirms (or auto-confirm is on), false otherwise.</returns>
    public static bool Confirm(string toolName, bool autoConfirm)
    {
        if (autoConfirm)
        {
            return true;
        }

        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return true;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{OutputFormatter.Warning.ToMarkup()}]The AI wants to execute:[/] [bold]{toolName.EscapeMarkup()}[/]");
        return AnsiConsole.Confirm("Allow this operation?", false);
    }
}
