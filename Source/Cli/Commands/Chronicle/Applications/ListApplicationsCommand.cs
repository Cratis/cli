// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Applications;

/// <summary>
/// Lists all applications (OAuth clients) registered in the Chronicle system.
/// </summary>
[CliCommand("list", "List all applications", Branch = typeof(ChronicleBranch.Applications))]
[CliExample("chronicle", "applications", "list")]
[LlmOutputAdvice("plain", "Use plain for consistency with other listing commands.")]
public class ListApplicationsCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
        var applications = await services.Applications.GetAll();

        OutputFormatter.Write(
            format,
            applications,
            ["Id", "ClientId", "Active", "Created"],
            app =>
            [
                app.Id.ToString(),
                app.ClientId,
                app.IsActive.ToString(),
                app.CreatedAt.ToString()
            ]);

        return ExitCodes.Success;
    }
}
