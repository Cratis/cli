// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502 // Marker type is intentionally empty

namespace Cratis.Cli.Registration;

/// <summary>
/// Build and test only what a change can reach.
/// </summary>
[CliBranch("affected", "Decide which projects to build and which tests to run for a change: select the affected test projects and tests, run them, refresh the coverage maps on merge, and replay merged pull requests to measure the selection.")]
public static class AffectedBranch;
