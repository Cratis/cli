// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Applications;

/// <summary>
/// Removes an application (OAuth client) from the Chronicle system.
/// </summary>
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
