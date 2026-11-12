// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502 // Marker type is intentionally empty

namespace Cratis.Cli.Registration;

/// <summary>
/// Extension packages a Screenplay model references: restoring, updating and vendoring them. Creating, verifying and running them join this group.
/// </summary>
[CliBranch("extension", "Work with the extension packages a Screenplay model references. Restore what screenplay.lock pins, update references to newer releases and vendor locked packages for air-gapped deployments.")]
public static class ExtensionBranch;
