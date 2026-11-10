// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A kit as fetched, with what is recorded about where it came from.
/// </summary>
/// <param name="Kit">The kit.</param>
/// <param name="Sha256">The SHA-256 of a registered kit's archive, or <see langword="null"/>.</param>
/// <param name="Source">The Git URL or folder of a kit that is not registered, free of credentials, or <see langword="null"/>.</param>
/// <param name="Commit">The commit a Git URL resolved to, or <see langword="null"/>.</param>
public sealed record BuildKitFetched(BuildKit Kit, string? Sha256, string? Source, string? Commit);
