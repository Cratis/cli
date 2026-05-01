// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Applications;

/// <summary>
/// Adds a new application (OAuth client) to the Chronicle system.
/// </summary>
[CliCommand("add", "Add a new application", Branch = typeof(ChronicleBranch.Applications))]
[CliExample("chronicle", "applications", "add", "my-app", "my-secret")]
[LlmOutputAdvice("plain", "Plain outputs a simple confirmation message.")]
[LlmOption("<CLIENT_ID>", "string", "The client identifier for the new application (positional)")]
[LlmOption("<CLIENT_SECRET>", "string", "The client secret for the new application (positional)")]
public class AddApplicationCommand : ChronicleCommand<AddApplicationSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, AddApplicationSettings settings, string format)
    {
        await services.Applications.Add(new AddApplication
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = settings.ClientId,
            ClientSecret = settings.ClientSecret
        });

        OutputFormatter.WriteMessage(format, $"Application '{settings.ClientId}' added.");
        return ExitCodes.Success;
    }
}
