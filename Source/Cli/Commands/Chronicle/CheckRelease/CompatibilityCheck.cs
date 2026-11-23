// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// One compatibility check of the cratis.compat/v1 result.
/// </summary>
/// <param name="Check">The check id.</param>
/// <param name="Expected">What the release needs, as human readable text.</param>
/// <param name="Actual">What was found on the target, as human readable text.</param>
/// <param name="Verdict">One of the <see cref="CompatibilityVerdict"/> values.</param>
/// <param name="Action">What to do when the verdict is not compatible; empty when it is.</param>
public record CompatibilityCheck(string Check, string Expected, string Actual, string Verdict, string Action)
{
    /// <summary>
    /// The id of the check of the kernel version.
    /// </summary>
    public const string Kernel = "chronicle-kernel";

    /// <summary>
    /// The id of the check of the wire contracts.
    /// </summary>
    public const string Contracts = "chronicle-contracts";

    /// <summary>
    /// The id of the check of the event type registrations.
    /// </summary>
    public const string Schemas = "event-schemas";

    /// <summary>
    /// Creates a compatible check.
    /// </summary>
    /// <param name="check">The check id.</param>
    /// <param name="expected">What the release needs.</param>
    /// <param name="actual">What was found.</param>
    /// <returns>The check.</returns>
    public static CompatibilityCheck Passed(string check, string expected, string actual) =>
        new(check, expected, actual, CompatibilityVerdict.Compatible, string.Empty);

    /// <summary>
    /// Creates an incompatible check.
    /// </summary>
    /// <param name="check">The check id.</param>
    /// <param name="expected">What the release needs.</param>
    /// <param name="actual">What was found.</param>
    /// <param name="action">What to do about it.</param>
    /// <returns>The check.</returns>
    public static CompatibilityCheck Failed(string check, string expected, string actual, string action) =>
        new(check, expected, actual, CompatibilityVerdict.Incompatible, action);

    /// <summary>
    /// Creates a check that could not be answered.
    /// </summary>
    /// <param name="check">The check id.</param>
    /// <param name="expected">What the release needs.</param>
    /// <param name="actual">What could be found out.</param>
    /// <param name="action">What to do about it.</param>
    /// <returns>The check.</returns>
    public static CompatibilityCheck Undetermined(string check, string expected, string actual, string action) =>
        new(check, expected, actual, CompatibilityVerdict.Unknown, action);
}
