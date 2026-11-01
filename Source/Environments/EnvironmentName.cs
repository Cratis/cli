// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// Validates environment names, which double as file names.
/// </summary>
public static class EnvironmentName
{
    /// <summary>
    /// The name used when no environment is selected.
    /// </summary>
    public const string Default = "development";

    /// <summary>
    /// Determines whether a name is a safe file name for an environment.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True when the name is valid.</returns>
    public static bool IsValid(string? name) =>
        !string.IsNullOrEmpty(name) && name.Length <= 64 && char.IsAsciiLetterOrDigit(name[0]) && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-') && !name.Contains("..", StringComparison.Ordinal);

    /// <summary>
    /// Throws when the name is not a valid environment name.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>The same name.</returns>
    /// <exception cref="InvalidEnvironmentNameException">Thrown when the name is invalid.</exception>
    public static string Validate(string? name) =>
        IsValid(name) ? name! : throw new InvalidEnvironmentNameException(name ?? string.Empty);
}
