// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Assesses source-framework package evidence without conflating it with source recognition or Screenplay lowering.
/// </summary>
static class ScreenplayCompatibility
{
    static readonly HashSet<(string Marten, string Wolverine)> _canonicalCritterStack =
    [
        ("6.3.0", "1.11.1"),
        ("9.20.0", "6.23.1"),
        ("9.20.1", "6.23.1"),
        ("9.23.0", "6.29.1")
    ];

    static readonly HashSet<string> _canonicalMarten = new(StringComparer.Ordinal)
    {
        "9.20.1"
    };

    /// <summary>
    /// Creates provider and compatibility provenance before source interpretation starts.
    /// </summary>
    /// <param name="provider">The selected provider.</param>
    /// <param name="loaded">The selected project compilations and workspace provenance.</param>
    /// <returns>The compatibility evaluation.</returns>
    public static ScreenplayCompatibilityEvaluation Evaluate(IScreenplaySourceProvider provider, LoadedCompilation loaded)
    {
        if (provider.Name == ScreenplayProviders.Arc)
        {
            return new ScreenplayCompatibilityEvaluation(
                new ScreenplayGenerationProvenance(provider.Name, provider.Version, loaded.ProjectProvenance, null),
                null);
        }

        var packages = loaded.ProjectProvenance.SelectMany(project => project.Packages).ToArray();
        var marten = VersionsOf(packages, "Marten");
        var wolverine = VersionsOf(packages, "WolverineFx");
        var wolverineMarten = VersionsOf(packages, "WolverineFx.Marten");
        var unknown = UnknownReason(provider.Name, marten, wolverine, wolverineMarten);
        if (unknown is not null)
        {
            return Blocked(
                provider,
                loaded,
                ScreenplaySupportTier.Unknown,
                ScreenplayRecognitionStatus.Unknown,
                ScreenplayDiagnosticCodes.UnknownFrameworkVersion,
                unknown);
        }

        var wolverineVersion = wolverine.Length == 0 ? null : wolverine[0];
        var unsupported = NewerMajorReason(provider.Name, marten[0], wolverineVersion);
        if (unsupported is not null)
        {
            return Blocked(
                provider,
                loaded,
                ScreenplaySupportTier.Unsupported,
                ScreenplayRecognitionStatus.Unsupported,
                ScreenplayDiagnosticCodes.UnsupportedFrameworkVersion,
                unsupported);
        }

        var unreviewed = UnreviewedMajorReason(provider.Name, marten[0], wolverineVersion);
        if (unreviewed is not null)
        {
            return Blocked(
                provider,
                loaded,
                ScreenplaySupportTier.Unknown,
                ScreenplayRecognitionStatus.Unknown,
                ScreenplayDiagnosticCodes.UnknownFrameworkVersion,
                unreviewed);
        }

        var packageSetIsCanonical = IsCanonicalPackageSet(provider.Name, marten[0], wolverineVersion, wolverineMarten);
        var providerCarriesCanonicalBaseline = ProviderCarriesCanonicalBaseline(provider.Version);
        var tier = packageSetIsCanonical && providerCarriesCanonicalBaseline
            ? ScreenplaySupportTier.Canonical
            : ScreenplaySupportTier.SourceReviewed;
        var packageSet = provider.Name == ScreenplayProviders.Marten
            ? $"Marten {marten[0]}"
            : $"Marten {marten[0]} with WolverineFx {wolverine[0]}";
        var explanation = ExplanationFor(tier, packageSetIsCanonical, packageSet, provider.Version);
        var report = new ScreenplayCompatibilityReport(
            tier,
            ScreenplayRecognitionStatus.Recognized,
            ScreenplaySemanticConformance.RequiresHumanReview,
            ScreenplayLoweringFidelity.NotEvaluated,
            explanation);

        return new ScreenplayCompatibilityEvaluation(
            new ScreenplayGenerationProvenance(provider.Name, provider.Version, loaded.ProjectProvenance, report),
            null);
    }

    static ScreenplayCompatibilityEvaluation Blocked(
        IScreenplaySourceProvider provider,
        LoadedCompilation loaded,
        ScreenplaySupportTier tier,
        ScreenplayRecognitionStatus recognition,
        string code,
        string explanation)
    {
        var report = new ScreenplayCompatibilityReport(
            tier,
            recognition,
            recognition == ScreenplayRecognitionStatus.Unsupported
                ? ScreenplaySemanticConformance.Unsupported
                : ScreenplaySemanticConformance.NotEvaluated,
            ScreenplayLoweringFidelity.NotEvaluated,
            explanation);
        var diagnostic = new ScreenplayDiagnostic(
            ScreenplayDiagnosticSeverity.Error,
            code,
            $"{explanation}. Generation stopped before source semantics were interpreted",
            null);
        return new ScreenplayCompatibilityEvaluation(
            new ScreenplayGenerationProvenance(provider.Name, provider.Version, loaded.ProjectProvenance, report),
            diagnostic);
    }

    static string[] VersionsOf(IEnumerable<ResolvedScreenplayPackage> packages, string packageId) =>
    [
        .. packages
            .Where(package => string.Equals(package.Id, packageId, StringComparison.OrdinalIgnoreCase))
            .Select(package => package.Version)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
    ];

    static string? UnknownReason(
        string provider,
        string[] marten,
        string[] wolverine,
        string[] wolverineMarten)
    {
        if (marten.Length == 0)
        {
            return "Marten was recognized in assembly metadata, but its resolved NuGet package version was not found for the selected target framework";
        }

        if (marten.Length > 1)
        {
            return $"Projects resolve divergent Marten versions: {string.Join(", ", marten)}";
        }

        if (MajorOf(marten[0]) is null)
        {
            return $"Marten version '{marten[0]}' cannot be classified";
        }

        if (provider != ScreenplayProviders.CritterStack)
        {
            return null;
        }

        if (wolverine.Length == 0)
        {
            return "Wolverine was recognized in assembly metadata, but its resolved WolverineFx package version was not found for the selected target framework";
        }

        if (wolverine.Length > 1)
        {
            return $"Projects resolve divergent WolverineFx versions: {string.Join(", ", wolverine)}";
        }

        if (MajorOf(wolverine[0]) is null)
        {
            return $"WolverineFx version '{wolverine[0]}' cannot be classified";
        }

        if (wolverineMarten.Length > 1)
        {
            return $"Projects resolve divergent WolverineFx.Marten versions: {string.Join(", ", wolverineMarten)}";
        }

        if (wolverineMarten.Length == 1 && !string.Equals(wolverineMarten[0], wolverine[0], StringComparison.OrdinalIgnoreCase))
        {
            return $"WolverineFx.Marten {wolverineMarten[0]} does not match WolverineFx {wolverine[0]}";
        }

        return wolverineMarten.Length == 1 && MajorOf(wolverineMarten[0]) is null
            ? $"WolverineFx.Marten version '{wolverineMarten[0]}' cannot be classified"
            : null;
    }

    static string? NewerMajorReason(string provider, string marten, string? wolverine)
    {
        if (MajorOf(marten)!.Value > 9)
        {
            return $"Marten {marten} is newer than the highest source-reviewed major (9)";
        }

        return provider == ScreenplayProviders.CritterStack && MajorOf(wolverine!)!.Value > 6
            ? $"WolverineFx {wolverine} is newer than the highest source-reviewed major (6)"
            : null;
    }

    static string? UnreviewedMajorReason(string provider, string marten, string? wolverine)
    {
        if (MajorOf(marten)!.Value is not 6 and not 9)
        {
            return $"Marten {marten} has no canonical or source-reviewed major-generation evidence";
        }

        return provider == ScreenplayProviders.CritterStack && MajorOf(wolverine!)!.Value is not 1 and not 6
            ? $"WolverineFx {wolverine} has no canonical or source-reviewed major-generation evidence"
            : null;
    }

    static string ExplanationFor(
        ScreenplaySupportTier tier,
        bool packageSetIsCanonical,
        string packageSet,
        string providerVersion)
    {
        if (tier == ScreenplaySupportTier.Canonical)
        {
            return $"{packageSet} matches a pinned canonical package set for bundled provider {providerVersion}; only fixture-asserted behaviors are canonical";
        }

        return packageSetIsCanonical
            ? $"{packageSet} is canonical for Critter Stack provider 0.3.0 or newer, but bundled provider {providerVersion} predates that complete baseline"
            : $"{packageSet} is within source-reviewed major generations but is not an exact canonical package set";
    }

    static bool IsCanonicalPackageSet(string provider, string marten, string? wolverine, string[] wolverineMarten) =>
        provider == ScreenplayProviders.Marten
            ? _canonicalMarten.Contains(marten)
            : _canonicalCritterStack.Contains((marten, wolverine!)) &&
              wolverineMarten.Length == 1 &&
              string.Equals(wolverineMarten[0], wolverine, StringComparison.OrdinalIgnoreCase);

    static bool ProviderCarriesCanonicalBaseline(string providerVersion) =>
        System.Version.TryParse(providerVersion, out var parsed) && parsed >= new System.Version(0, 3, 0);

    static int? MajorOf(string version)
    {
        var separator = version.IndexOfAny(['-', '+']);
        if (separator == version.Length - 1)
        {
            return null;
        }

        var core = separator < 0 ? version : version[..separator];
        return System.Version.TryParse(core, out var parsed) ? parsed.Major : null;
    }
}

/// <summary>
/// Represents compatibility admission before generation and finalizes lowering evidence afterwards.
/// </summary>
/// <param name="Provenance">The pre-generation provenance report.</param>
/// <param name="BlockingDiagnostic">The diagnostic that prevents generation, when compatibility is not admitted.</param>
sealed record ScreenplayCompatibilityEvaluation(
    ScreenplayGenerationProvenance Provenance,
    ScreenplayDiagnostic? BlockingDiagnostic)
{
    /// <summary>
    /// Finalizes semantic and lowering dimensions from generation diagnostics.
    /// </summary>
    /// <param name="diagnostics">Everything reported while interpreting and lowering source.</param>
    /// <returns>The finalized provenance.</returns>
    public ScreenplayGenerationProvenance Complete(IReadOnlyList<ScreenplayDiagnostic> diagnostics)
    {
        if (Provenance.Compatibility is not { } compatibility)
        {
            return Provenance;
        }

        var failed = diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
        var lossReported = diagnostics.Any(diagnostic =>
            diagnostic.Code.StartsWith("GEN", StringComparison.Ordinal) ||
            diagnostic.Code.StartsWith("MARTEN", StringComparison.Ordinal) ||
            diagnostic.Code.StartsWith("WOLVERINE", StringComparison.Ordinal) ||
            diagnostic.Code.StartsWith("CRITTER", StringComparison.Ordinal) ||
            diagnostic.Code == ScreenplayDiagnosticCodes.UnsupportedGenerationOption);
        return Provenance with
        {
            Compatibility = compatibility with
            {
                SemanticConformance = failed
                    ? ScreenplaySemanticConformance.Contradictory
                    : ScreenplaySemanticConformance.RequiresHumanReview,
                LoweringFidelity = LoweringFidelityFor(failed, lossReported)
            }
        };
    }

    static ScreenplayLoweringFidelity LoweringFidelityFor(bool failed, bool lossReported)
    {
        if (failed)
        {
            return ScreenplayLoweringFidelity.Failed;
        }

        return lossReported ? ScreenplayLoweringFidelity.LossReported : ScreenplayLoweringFidelity.NoReportedLoss;
    }
}
