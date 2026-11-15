// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// What a verification may compare the package with.
/// </summary>
/// <param name="Locked">The entry <c language="text">screenplay.lock</c> pins for the package, if it is pinned.</param>
/// <param name="PackageDigest">The digest of the package as the registry served it, if it was fetched from one.</param>
/// <param name="Baseline">The contract of the release this one replaces, if the package must be a drop-in provider for it.</param>
/// <param name="CompilerVersion">The version of the Screenplay compiler, for <c language="text">requires screenplay</c>.</param>
public record ExtensionVerifyOptions(ExtensionLockEntry? Locked = null, string? PackageDigest = null, ExtensionContract? Baseline = null, string? CompilerVersion = null);
