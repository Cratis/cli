// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Validates and normalizes CLI-owned source-structure options before they reach generation or provenance.
/// </summary>
static class ScreenplaySourceStructureOptions
{
    /// <summary>
    /// Validates and normalizes the project-relative feature root using the shared .NET placement path contract.
    /// </summary>
    /// <param name="options">The requested generation options.</param>
    /// <param name="normalizedOptions">The options carrying the normalized feature root when valid.</param>
    /// <returns><see langword="true"/> when the feature root is absent or valid.</returns>
    internal static bool TryNormalize(
        ScreenplayGenerationOptions options,
        out ScreenplayGenerationOptions normalizedOptions)
    {
        normalizedOptions = options;
        if (options.FeatureRoot is null)
        {
            return true;
        }

        string normalized;
        try
        {
            normalized = options.FeatureRoot.Normalize().Replace('\\', '/');
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Any(char.IsControl) ||
            normalized[0] == '/' ||
            (normalized.Length >= 2 && char.IsLetter(normalized[0]) && normalized[1] == ':'))
        {
            return false;
        }

        var parts = new List<string>();
        foreach (var part in normalized.Split('/'))
        {
            if (string.IsNullOrEmpty(part) || string.Equals(part, ".", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(part, "..", StringComparison.Ordinal) || !IsName(part))
            {
                return false;
            }

            parts.Add(part);
        }

        if (parts.Count == 0)
        {
            return false;
        }

        normalizedOptions = options with { FeatureRoot = string.Join('/', parts) };
        return true;
    }

    /// <summary>
    /// Creates the stable, non-disclosing diagnostic for an invalid feature root.
    /// </summary>
    /// <param name="targetPath">The source target path.</param>
    /// <returns>The blocking feature-root diagnostic.</returns>
    internal static ScreenplayDiagnostic InvalidFeatureRoot(string targetPath) =>
        new(
            ScreenplayDiagnosticSeverity.Error,
            DotNetSourceStructureDiagnosticCodes.InvalidPath,
            "The project-relative feature root is invalid",
            ScreenplayDiagnosticLocations.Target(targetPath));

    static bool IsName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
        !value.Any(char.IsControl) &&
        value is not "." and not "..";
}
