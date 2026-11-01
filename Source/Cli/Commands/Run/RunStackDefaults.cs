// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The image tags a run with authentication starts when none is asked for.
/// </summary>
public static class RunStackDefaults
{
    /// <summary>
    /// The AuthProxy tag. It follows the latest release, because AuthProxy's application contract is additive and a run is disposable.
    /// </summary>
    public const string AuthProxyTag = "latest";

    /// <summary>
    /// The Ante tag: the first release that runs from the onboarding contract.
    /// </summary>
    public const string AnteTag = "1.5.0";
}
