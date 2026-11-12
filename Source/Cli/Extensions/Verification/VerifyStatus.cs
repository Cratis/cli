// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// What happened to one check.
/// </summary>
public enum VerifyStatus
{
    /// <summary>The check ran and found nothing wrong.</summary>
    Passed,

    /// <summary>The check ran and found something wrong.</summary>
    Failed,

    /// <summary>The check applies to this package but could not run. This is never reported as a pass; <c language="bash">--strict</c> fails it.</summary>
    Skipped,

    /// <summary>The package has nothing for the check to look at.</summary>
    NotApplicable
}
