// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Applications;

/// <summary>
/// Removes an application (OAuth client) from the Chronicle system.
/// </summary>
[CliCommand("remove", "Remove an application", Branch = typeof(ChronicleBranch.Applications), DynamicCompletion = "applications")]
[CliExample("chronicle", "applications", "remove", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOutputAdvice("plain", "Plain outputs a simple confirmation message.")]
[LlmOption("<APP_ID>", "guid", "The unique identifier of the application to remove (positional)")]
public class RemoveApplicationCommand : ChronicleCommand<RemoveApplicationSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, RemoveApplicationSettings settings, string format)
    {
        if (!ConfirmationHelper.ShouldProceed(settings, $"Are you sure you want to remove application '{settings.AppId}'?"))
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return ExitCodes.Success;
        }

        await services.Applications.Remove(new RemoveApplication
        {
            Id = settings.AppId
        });

        OutputFormatter.WriteMessage(format, $"Application '{settings.AppId}' removed.");
        return ExitCodes.Success;
    }
}
