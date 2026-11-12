// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// The result of verifying one package.
/// </summary>
/// <param name="Target">What was verified: a folder or a reference.</param>
/// <param name="Checks">The checks, in the order they ran.</param>
public record ExtensionVerifyReport(string Target, IReadOnlyList<VerifyCheckResult> Checks)
{
    /// <summary>
    /// Gets every finding of every check.
    /// </summary>
    public IEnumerable<VerifyDiagnostic> Diagnostics => Checks.SelectMany(check => check.Diagnostics);

    /// <summary>
    /// Gets a value indicating whether the package is correct: no check found an error.
    /// </summary>
    public bool Passed => Checks.All(check => check.Status != VerifyStatus.Failed);

    /// <summary>
    /// Gets a value indicating whether the package is correct and nothing needs attention: no error, no warning and no check that could not run.
    /// </summary>
    public bool PassedStrictly => Passed && !Diagnostics.Any(diagnostic => diagnostic.Severity == VerifySeverity.Warning) && Checks.All(check => check.Status != VerifyStatus.Skipped);
}
