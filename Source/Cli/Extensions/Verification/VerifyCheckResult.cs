// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// The result of one check.
/// </summary>
/// <param name="Check">The check name.</param>
/// <param name="Status">What happened.</param>
/// <param name="Diagnostics">What it found.</param>
public record VerifyCheckResult(string Check, VerifyStatus Status, IReadOnlyList<VerifyDiagnostic> Diagnostics);
