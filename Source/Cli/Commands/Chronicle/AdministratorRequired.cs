// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Describes and detects Chronicle refusing an operation because the caller is not an administrator.
/// </summary>
public static class AdministratorRequired
{
    /// <summary>
    /// The hint shown when Chronicle refuses an administrator-only command.
    /// </summary>
    public const string Hint = "This command needs a Chronicle administrator. Run 'cratis chronicle login <administrator-username>' to sign in as one.";

    /// <summary>
    /// The message a Workbench list shows in place of its rows when Chronicle refused the list.
    /// </summary>
    public const string EmptyStateMessage = "Requires an administrator — run cratis chronicle login.";

    /// <summary>
    /// Checks whether a gRPC failure is the server refusing an authenticated caller for lack of privileges.
    /// An unauthenticated failure is not a denial: it means the token or credentials were not accepted, and is handled as an authentication error.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True when the server refused the call for lack of privileges.</returns>
    public static bool IsDenial(RpcException exception) => exception.StatusCode is StatusCode.PermissionDenied;

    /// <summary>
    /// Checks whether a gRPC failure should make a Workbench list show the login guidance: the server either refused the caller for lack of privileges
    /// or did not accept its token or credentials (for example an expired login).
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True when the list should show <see cref="EmptyStateMessage"/>.</returns>
    public static bool NeedsLogin(RpcException exception) => exception.StatusCode is StatusCode.PermissionDenied or StatusCode.Unauthenticated;

    /// <summary>
    /// Gets the hint to show for a server failure.
    /// </summary>
    /// <param name="requiresAdministrator">Whether the command needs an administrator.</param>
    /// <param name="isAuthorized">Whether the server authorized the call.</param>
    /// <returns>The hint, or <see langword="null"/> when it does not apply.</returns>
    public static string? HintFor(bool requiresAdministrator, bool isAuthorized) =>
        requiresAdministrator && !isAuthorized ? Hint : null;
}
