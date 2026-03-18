// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Defines standard exit codes returned by CLI commands.
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// The command completed successfully.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// The command failed due to invalid input or a resource was not found.
    /// </summary>
    public const int NotFound = 1;

    /// <summary>
    /// The command could not connect to the Chronicle server.
    /// </summary>
    public const int ConnectionError = 2;

    /// <summary>
    /// The server returned an unexpected error.
    /// </summary>
    public const int ServerError = 3;

    /// <summary>
    /// Authentication failed or credentials are missing.
    /// </summary>
    public const int AuthenticationError = 4;

    /// <summary>
    /// The command failed due to a validation error (e.g. duplicate name, invalid state).
    /// </summary>
    public const int ValidationError = 5;

    /// <summary>
    /// Machine-parseable error code string for success.
    /// </summary>
    public const string SuccessCode = "success";

    /// <summary>
    /// Machine-parseable error code string for not found.
    /// </summary>
    public const string NotFoundCode = "not_found";

    /// <summary>
    /// Machine-parseable error code string for connection errors.
    /// </summary>
    public const string ConnectionErrorCode = "connection_error";

    /// <summary>
    /// Machine-parseable error code string for server errors.
    /// </summary>
    public const string ServerErrorCode = "server_error";

    /// <summary>
    /// Machine-parseable error code string for authentication errors.
    /// </summary>
    public const string AuthenticationErrorCode = "authentication_error";

    /// <summary>
    /// Machine-parseable error code string for validation errors.
    /// </summary>
    public const string ValidationErrorCode = "validation_error";

    /// <summary>
    /// Returns the machine-parseable error code string for the given exit code integer.
    /// </summary>
    /// <param name="exitCode">The integer exit code.</param>
    /// <returns>The corresponding string error code.</returns>
    public static string CodeFor(int exitCode) => exitCode switch
    {
        Success => SuccessCode,
        NotFound => NotFoundCode,
        ConnectionError => ConnectionErrorCode,
        ServerError => ServerErrorCode,
        AuthenticationError => AuthenticationErrorCode,
        ValidationError => ValidationErrorCode,
        _ => "unknown"
    };
}
