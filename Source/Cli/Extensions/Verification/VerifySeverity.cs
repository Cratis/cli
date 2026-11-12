// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// How serious a finding is.
/// </summary>
public enum VerifySeverity
{
    /// <summary>The package is not correct.</summary>
    Error,

    /// <summary>The package is correct but could be better; it fails only with <c language="bash">--strict</c>.</summary>
    Warning
}
