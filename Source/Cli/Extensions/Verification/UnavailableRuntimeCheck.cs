// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// Stands in for a runtime check the CLI cannot run yet. It says so, as a skipped check, and never reports a pass.
/// </summary>
/// <param name="check">The check name.</param>
/// <param name="code">The diagnostic code.</param>
/// <param name="reason">Why the check cannot run.</param>
public class UnavailableRuntimeCheck(string check, string code, string reason) : IExtensionRuntimeCheck
{
    /// <inheritdoc/>
    public Task<VerifyCheckResult> Run(ExtensionContract contract, string packageRoot, CancellationToken cancellationToken) =>
        Task.FromResult(new VerifyCheckResult(check, VerifyStatus.Skipped, [new(check, code, VerifySeverity.Warning, reason)]));
}
