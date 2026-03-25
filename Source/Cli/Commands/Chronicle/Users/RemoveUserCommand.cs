// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Users;

/// <summary>
/// Removes a user from the Chronicle system.
/// </summary>
[CliCommand("remove", "Remove a user", Branch = typeof(ChronicleBranch.Users))]
[CliExample("chronicle", "users", "remove", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOutputAdvice("plain", "Plain outputs a simple confirmation message.")]
[LlmOption("<USER_ID>", "guid", "The unique identifier of the user to remove (positional)")]
public class RemoveUserCommand : ChronicleCommand<RemoveUserSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, RemoveUserSettings settings, string format)
    {
        if (!ConfirmationHelper.ShouldProceed(settings, $"Are you sure you want to remove user '{settings.UserId}'?"))
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return ExitCodes.Success;
        }

        await services.Users.Remove(new RemoveUser
        {
            UserId = settings.UserId
        });

        OutputFormatter.WriteMessage(format, $"User '{settings.UserId}' removed.");
        return ExitCodes.Success;
    }
}
