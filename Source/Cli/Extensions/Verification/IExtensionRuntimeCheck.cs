// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// A check that has to run the package's code: the frontend against the Scene conformance kit, or the backend through the protocol handshake.
/// </summary>
public interface IExtensionRuntimeCheck
{
    /// <summary>
    /// Runs the check.
    /// </summary>
    /// <param name="contract">The package's contract.</param>
    /// <param name="packageRoot">The package folder.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The result, with <see cref="VerifyStatus.Skipped"/> when the check could not run.</returns>
    Task<VerifyCheckResult> Run(ExtensionContract contract, string packageRoot, CancellationToken cancellationToken);
}
