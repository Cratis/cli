// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// Creates the frontend and backend runtime checks in one place, so a real check replaces a stand-in with one line.
/// </summary>
public static class RuntimeChecks
{
    /// <summary>
    /// Creates the frontend check.
    /// </summary>
    /// <returns>The <see cref="IExtensionRuntimeCheck"/>.</returns>
    public static IExtensionRuntimeCheck Frontend() =>
        new UnavailableRuntimeCheck(ExtensionVerifyCodes.FrontendCheck, ExtensionVerifyCodes.FrontendNotRun, "The components were not registered or run against the Scene conformance kit (@cratis/scene.extensions): the CLI does not run frontends yet. Run the kit in the package's own specifications.");

    /// <summary>
    /// Creates the backend check.
    /// </summary>
    /// <returns>The <see cref="IExtensionRuntimeCheck"/>.</returns>
    public static IExtensionRuntimeCheck Backend() =>
        new UnavailableRuntimeCheck(ExtensionVerifyCodes.BackendCheck, ExtensionVerifyCodes.BackendNotRun, "The backend was not started and no protocol handshake was made against the contract: the CLI does not run backends yet. Stage performs the handshake when it loads the package.");

    /// <summary>
    /// Creates a verifier with the runtime checks the CLI has today.
    /// </summary>
    /// <returns>The <see cref="ExtensionVerifier"/>.</returns>
    public static ExtensionVerifier CreateVerifier() => new(Frontend(), Backend());
}
