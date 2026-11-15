// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502 // Marker type is intentionally empty

namespace Cratis.Cli.Registration;

/// <summary>
/// Extension packages: creating and verifying them, and restoring, updating and vendoring the ones a Screenplay model references.
/// </summary>
[CliBranch("extension", "Create and verify extension packages, and work with the ones a Screenplay model references. Restore what screenplay.lock pins, update references to newer releases and vendor locked packages for air-gapped deployments.")]
public static class ExtensionBranch;
