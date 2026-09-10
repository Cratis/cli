// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Helper for prompting the user for confirmation before destructive operations.
/// </summary>
public static class ConfirmationHelper
{
    /// <summary>
    /// Determines the confirmation outcome by checking the --yes flag or prompting an interactive user.
    /// </summary>
    /// <param name="settings">The global settings containing the Yes flag.</param>
    /// <param name="prompt">The confirmation prompt to display.</param>
    /// <returns>The typed confirmation outcome.</returns>
    public static ConfirmationOutcome Confirm(GlobalSettings settings, string prompt) =>
        Confirm(
            settings,
            GlobalSettings.IsInteractiveEnvironment(),
            defaultValue => AnsiConsole.Confirm(prompt, defaultValue));

    /// <summary>
    /// Confirms an operation and centralizes command output and exit-code behavior for non-confirmed outcomes.
    /// </summary>
    /// <param name="settings">The global settings containing the Yes flag.</param>
    /// <param name="prompt">The confirmation prompt to display.</param>
    /// <param name="format">The resolved output format.</param>
    /// <returns>Null when the operation was confirmed; otherwise the command exit code to return.</returns>
    public static int? ConfirmOrExit(GlobalSettings settings, string prompt, string format) =>
        ExitCodeFor(Confirm(settings, prompt), format);

    internal static ConfirmationOutcome Confirm(GlobalSettings settings, bool isInteractiveEnvironment, Func<bool, bool> confirm)
    {
        if (settings.Yes)
        {
            return ConfirmationOutcome.Confirmed;
        }

        if (!isInteractiveEnvironment)
        {
            return ConfirmationOutcome.ConfirmationRequired;
        }

        return confirm(false) ? ConfirmationOutcome.Confirmed : ConfirmationOutcome.Declined;
    }

    internal static int? ExitCodeFor(ConfirmationOutcome outcome, string format)
    {
        if (outcome is ConfirmationOutcome.Confirmed)
        {
            return null;
        }

        if (outcome is ConfirmationOutcome.Declined)
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return ExitCodes.Success;
        }

        OutputFormatter.WriteError(
            format,
            "Confirmation is required for this destructive command in a non-interactive environment",
            "Re-run the command with --yes to confirm the operation",
            ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }
}
