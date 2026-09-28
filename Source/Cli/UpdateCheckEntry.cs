// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Represents what an update check last learned from one source.
/// </summary>
/// <param name="LatestVersion">The latest version the source reported, or null when it never answered.</param>
/// <param name="CheckedAt">When the source last answered.</param>
/// <param name="RetryAfter">When the source may be asked again after a failed attempt, or null when the last attempt succeeded.</param>
internal sealed record UpdateCheckEntry(string? LatestVersion, DateTime CheckedAt, DateTime? RetryAfter = null);
