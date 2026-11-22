// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// The result of checking a release against a Chronicle target.
/// </summary>
/// <param name="Release">The release description the result is about.</param>
/// <param name="EventStore">The event store the registrations were checked against.</param>
/// <param name="ReadAt">When the target was read.</param>
/// <param name="Checks">The checks, in stable order.</param>
public record CompatibilityResult(ReleaseDescription Release, string EventStore, DateTimeOffset ReadAt, IReadOnlyList<CompatibilityCheck> Checks)
{
    /// <summary>
    /// Gets the overall verdict: incompatible outranks unknown, and compatible only when every check passed.
    /// </summary>
    public string Verdict
    {
        get
        {
            if (Checks.Any(_ => _.Verdict == CompatibilityVerdict.Incompatible))
            {
                return CompatibilityVerdict.Incompatible;
            }

            return Checks.All(_ => _.Verdict == CompatibilityVerdict.Compatible) ? CompatibilityVerdict.Compatible : CompatibilityVerdict.Unknown;
        }
    }

    /// <summary>
    /// Gets the CLI exit code for the verdict.
    /// </summary>
    public int ExitCode => Verdict switch
    {
        CompatibilityVerdict.Compatible => ExitCodes.Success,
        CompatibilityVerdict.Incompatible => ExitCodes.ValidationError,
        _ => ExitCodes.ServerError
    };
}
