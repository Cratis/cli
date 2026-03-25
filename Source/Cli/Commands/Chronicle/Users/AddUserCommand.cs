// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Users;

/// <summary>
/// Adds a new user to the Chronicle system.
/// </summary>
[CliCommand("add", "Add a new user", Branch = typeof(ChronicleBranch.Users))]
[CliExample("chronicle", "users", "add", "alice", "alice@example.com", "P@ssw0rd!")]
[LlmOutputAdvice("plain", "Plain outputs a simple confirmation message.")]
[LlmOption("<USERNAME>", "string", "The username for the new user (positional)")]
[LlmOption("<EMAIL>", "string", "The email address for the new user (positional)")]
[LlmOption("<PASSWORD>", "string", "The initial password for the new user (positional)")]
public class AddUserCommand : ChronicleCommand<AddUserSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, AddUserSettings settings, string format)
    {
        await services.Users.Add(new AddUser
        {
            UserId = Guid.NewGuid(),
            Username = settings.Username,
            Email = settings.Email,
            Password = settings.Password
        });

        OutputFormatter.WriteMessage(format, $"User '{settings.Username}' added.");
        return ExitCodes.Success;
    }
}
