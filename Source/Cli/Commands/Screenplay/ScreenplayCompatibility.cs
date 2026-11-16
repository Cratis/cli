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

    static readonly HashSet<string> _canonicalMarten =
    [
with(StringComparer.Ordinal),         "9.20.1"
    ];

    /// <summary>
    /// Cratis.CritterStack.Screenplay 0.26.1: README.md, Compatibility fixtures, VogenConcepts.
    /// </summary>
    static readonly (string Marten, string Wolverine, string Vogen) _canonicalVogenCritterStack =
        ("9.45.0", "6.45.0", "8.0.7");

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

        var versions = PackageVersions.From(provider.Name, loaded);
        return BlockingReasonFor(versions) is { } reason
            ? Blocked(provider, loaded, reason)
            : Recognized(provider, loaded, versions);
    }

    static ScreenplayCompatibilityEvaluation Recognized(IScreenplaySourceProvider provider, LoadedCompilation loaded, PackageVersions versions)
    {
        var packageSetIsCanonical = IsCanonicalPackageSet(versions);
        var baseline = new ProviderBaseline(provider.Version, versions.Vogen.Count == 0 ? new System.Version(0, 3, 0) : new System.Version(0, 15, 0));
        var tier = packageSetIsCanonical && baseline.IsCarried
            ? ScreenplaySupportTier.Canonical
            : ScreenplaySupportTier.SourceReviewed;
        var explanation = ExplanationFor(tier, packageSetIsCanonical, versions.Describe(), baseline);
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

    static BlockingReason? BlockingReasonFor(PackageVersions versions)
    {
        if (UnknownReason(versions) is { } unknown)
        {
            return UnknownBlock(unknown);
        }

        if (NewerMajorReason(versions) is { } unsupported)
        {
            return new BlockingReason(
                ScreenplaySupportTier.Unsupported,
                ScreenplayRecognitionStatus.Unsupported,
                ScreenplayDiagnosticCodes.UnsupportedFrameworkVersion,
                unsupported);
        }

        return UnreviewedMajorReason(versions) is { } unreviewed ? UnknownBlock(unreviewed) : null;
    }

    static BlockingReason UnknownBlock(string explanation) => new(
        ScreenplaySupportTier.Unknown,
        ScreenplayRecognitionStatus.Unknown,
        ScreenplayDiagnosticCodes.UnknownFrameworkVersion,
        explanation);

    static ScreenplayCompatibilityEvaluation Blocked(IScreenplaySourceProvider provider, LoadedCompilation loaded, BlockingReason reason)
    {
        var report = new ScreenplayCompatibilityReport(
            reason.Tier,
            reason.Recognition,
            reason.Recognition == ScreenplayRecognitionStatus.Unsupported
                ? ScreenplaySemanticConformance.Unsupported
                : ScreenplaySemanticConformance.NotEvaluated,
            ScreenplayLoweringFidelity.NotEvaluated,
            reason.Explanation);
        var diagnostic = new ScreenplayDiagnostic(
            ScreenplayDiagnosticSeverity.Error,
            reason.Code,
            $"{reason.Explanation}. Generation stopped before source semantics were interpreted",
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

    static string? UnknownReason(PackageVersions versions) =>
        MartenUnknownReason(versions.Marten)
        ?? (versions.IsCritterStack ? WolverineUnknownReason(versions.Wolverine, versions.WolverineMarten) : null)
        ?? VogenUnknownReason(versions.Vogen, versions.HasVogenApiEvidence);

    static string? MartenUnknownReason(ScreenplayPackageEvidence marten)
    {
        if (marten.Count == 0)
        {
            return "Marten was recognized in assembly metadata, but its resolved NuGet package version was not found for the selected target framework";
        }

        return marten.UnknownReason;
    }

    static string? WolverineUnknownReason(ScreenplayPackageEvidence wolverine, ScreenplayPackageEvidence wolverineMarten)
    {
        if (wolverine.Count == 0)
        {
            return "Wolverine was recognized in assembly metadata, but its resolved WolverineFx package version was not found for the selected target framework";
        }

        return wolverine.UnknownReason ?? WolverineMartenUnknownReason(wolverine, wolverineMarten);
    }

    static string? WolverineMartenUnknownReason(ScreenplayPackageEvidence wolverine, ScreenplayPackageEvidence wolverineMarten)
    {
        if (wolverineMarten.Count == 0)
        {
            return null;
        }

        return wolverineMarten.DivergentReason
            ?? wolverineMarten.MismatchReason(wolverine)
            ?? wolverineMarten.UnclassifiedReason;
    }

    static string? VogenUnknownReason(ScreenplayPackageEvidence vogen, bool hasVogenApiEvidence)
    {
        if (vogen.Count == 0)
        {
            return hasVogenApiEvidence
                ? "Vogen was recognized in assembly metadata or API capabilities, but its resolved NuGet package version was not found for the selected target framework"
                : null;
        }

        return vogen.UnknownReason;
    }

    static string? NewerMajorReason(PackageVersions versions) =>
        versions.Marten.NewerMajorReason
        ?? (versions.IsCritterStack ? versions.Wolverine.NewerMajorReason : null)
        ?? (versions.Vogen.Count == 1 ? versions.Vogen.NewerMajorReason : null);

    static string? UnreviewedMajorReason(PackageVersions versions) =>
        versions.Marten.UnreviewedMajorReason
        ?? (versions.IsCritterStack ? versions.Wolverine.UnreviewedMajorReason : null)
        ?? (versions.Vogen.Count == 1 ? versions.Vogen.UnreviewedMajorReason : null);

    static string ExplanationFor(
        ScreenplaySupportTier tier,
        bool packageSetIsCanonical,
        string packageSet,
        ProviderBaseline baseline)
    {
        if (tier == ScreenplaySupportTier.Canonical)
        {
            return $"{packageSet} matches a pinned canonical package set for bundled provider {baseline.ProviderVersion}; only fixture-asserted behaviors are canonical";
        }

        return packageSetIsCanonical
            ? $"{packageSet} is canonical for Critter Stack provider {baseline.Canonical} or newer, but bundled provider {baseline.ProviderVersion} predates that complete baseline"
            : $"{packageSet} is within source-reviewed major generations but is not an exact canonical package set";
    }

    static bool IsCanonicalPackageSet(PackageVersions versions)
    {
        if (versions.Vogen.Count == 1)
        {
            return versions.IsCritterStack &&
                   (versions.Marten.Version, versions.WolverineVersion, versions.Vogen.Version) == _canonicalVogenCritterStack &&
                   versions.WolverineMarten.Count == 0;
        }

        return versions.Provider == ScreenplayProviders.Marten
            ? _canonicalMarten.Contains(versions.Marten.Version)
            : IsCanonicalCritterStack(versions);
    }

    static bool IsCanonicalCritterStack(PackageVersions versions) =>
        _canonicalCritterStack.Contains((versions.Marten.Version, versions.WolverineVersion!)) &&
        versions.WolverineMarten.Count == 1 &&
        string.Equals(versions.WolverineMarten.Version, versions.WolverineVersion, StringComparison.OrdinalIgnoreCase);

    sealed record BlockingReason(
        ScreenplaySupportTier Tier,
        ScreenplayRecognitionStatus Recognition,
        string Code,
        string Explanation);

    sealed record ProviderBaseline(string ProviderVersion, System.Version Canonical)
    {
        public bool IsCarried => System.Version.TryParse(ProviderVersion, out var parsed) && parsed >= Canonical;
    }

    sealed record PackageVersions
    {
        public required string Provider { get; init; }

        public required ScreenplayPackageEvidence Marten { get; init; }

        public required ScreenplayPackageEvidence Wolverine { get; init; }

        public required ScreenplayPackageEvidence WolverineMarten { get; init; }

        public required ScreenplayPackageEvidence Vogen { get; init; }

        public required bool HasVogenApiEvidence { get; init; }

        public bool IsCritterStack => Provider == ScreenplayProviders.CritterStack;

        public string? WolverineVersion => Wolverine.Count == 0 ? null : Wolverine.Version;

        public static PackageVersions From(string provider, LoadedCompilation loaded)
        {
            var packages = loaded.ProjectProvenance.SelectMany(project => project.Packages).ToArray();
            var hasVogenApiEvidence = loaded.ProjectProvenance.Any(project =>
                project.Assemblies.Any(assembly => string.Equals(assembly.Name, "Vogen", StringComparison.OrdinalIgnoreCase)) ||
                project.Capabilities.Contains("vogen.value-object", StringComparer.Ordinal));
            return new PackageVersions
            {
                Provider = provider,
                Marten = new("Marten", VersionsOf(packages, "Marten"), [6, 9]),
                Wolverine = new("WolverineFx", VersionsOf(packages, "WolverineFx"), [1, 6]),
                WolverineMarten = new("WolverineFx.Marten", VersionsOf(packages, "WolverineFx.Marten"), []),
                Vogen = new("Vogen", VersionsOf(packages, "Vogen"), [8]),
                HasVogenApiEvidence = hasVogenApiEvidence
            };
        }

        public string Describe()
        {
            var packageSet = Provider == ScreenplayProviders.Marten
                ? $"Marten {Marten.Version}"
                : $"Marten {Marten.Version} with WolverineFx {Wolverine.Version}";
            return Vogen.Count == 1 ? $"{packageSet} and Vogen {Vogen.Version}" : packageSet;
        }
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
            diagnostic.Code.StartsWith("VOG", StringComparison.Ordinal) ||
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
