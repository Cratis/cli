// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// The exception that is thrown when a logged-in user's access token is missing or expired.
/// </summary>
/// <param name="username">The user whose session has expired.</param>
public sealed class LoginSessionExpired(string username)
    : Exception($"The login session for '{username}' has expired or has no valid token. Run 'cratis chronicle login {username}' again.");
