// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Helper for prompting the user for confirmation before destructive operations.
/// </summary>
public static class ConfirmationHelper
{
    /// <summary>
    /// Determines whether the operation should proceed by checking the --yes flag,
    /// terminal interactivity, or prompting the user.
    /// </summary>
    /// <param name="settings">The global settings containing the Yes flag.</param>
    /// <param name="prompt">The confirmation prompt to display.</param>
    /// <returns>True if the operation should proceed; false if the user declined.</returns>
    public static bool ShouldProceed(GlobalSettings settings, string prompt)
    {
        if (settings.Yes)
        {
            return true;
        }

        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return true;
        }

        return AnsiConsole.Confirm(prompt);
    }
}
