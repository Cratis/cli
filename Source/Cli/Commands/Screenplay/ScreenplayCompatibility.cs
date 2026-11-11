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
        var baseline = new ProviderBaseline(provider.Version, versions.Vogen.Length == 0 ? new System.Version(0, 3, 0) : new System.Version(0, 15, 0));
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

    static string? MartenUnknownReason(string[] marten)
    {
        if (marten.Length == 0)
        {
            return "Marten was recognized in assembly metadata, but its resolved NuGet package version was not found for the selected target framework";
        }

        return DivergentReason("Marten", marten) ?? UnclassifiedReason("Marten", marten[0]);
    }

    static string? WolverineUnknownReason(string[] wolverine, string[] wolverineMarten)
    {
        if (wolverine.Length == 0)
        {
            return "Wolverine was recognized in assembly metadata, but its resolved WolverineFx package version was not found for the selected target framework";
        }

        return DivergentReason("WolverineFx", wolverine)
            ?? UnclassifiedReason("WolverineFx", wolverine[0])
            ?? WolverineMartenUnknownReason(wolverine[0], wolverineMarten);
    }

    static string? WolverineMartenUnknownReason(string wolverine, string[] wolverineMarten)
    {
        if (wolverineMarten.Length == 0)
        {
            return null;
        }

        return DivergentReason("WolverineFx.Marten", wolverineMarten)
            ?? MismatchReason(wolverine, wolverineMarten[0])
            ?? UnclassifiedReason("WolverineFx.Marten", wolverineMarten[0]);
    }

    static string? VogenUnknownReason(string[] vogen, bool hasVogenApiEvidence)
    {
        if (vogen.Length == 0)
        {
            return hasVogenApiEvidence
                ? "Vogen was recognized in assembly metadata or API capabilities, but its resolved NuGet package version was not found for the selected target framework"
                : null;
        }

        return DivergentReason("Vogen", vogen) ?? UnclassifiedReason("Vogen", vogen[0]);
    }

    static string? DivergentReason(string package, string[] versions) =>
        versions.Length > 1 ? $"Projects resolve divergent {package} versions: {string.Join(", ", versions)}" : null;

    static string? UnclassifiedReason(string package, string version) =>
        MajorOf(version) is null ? $"{package} version '{version}' cannot be classified" : null;

    static string? MismatchReason(string wolverine, string wolverineMarten) =>
        string.Equals(wolverineMarten, wolverine, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"WolverineFx.Marten {wolverineMarten} does not match WolverineFx {wolverine}";

    static string? NewerMajorReason(PackageVersions versions) =>
        NewerThan("Marten", versions.MartenVersion, 9)
        ?? (versions.IsCritterStack ? NewerThan("WolverineFx", versions.WolverineVersion!, 6) : null)
        ?? (versions.Vogen.Length == 1 ? NewerThan("Vogen", versions.Vogen[0], 8) : null);

    static string? UnreviewedMajorReason(PackageVersions versions) =>
        Unreviewed("Marten", versions.MartenVersion, [6, 9])
        ?? (versions.IsCritterStack ? Unreviewed("WolverineFx", versions.WolverineVersion!, [1, 6]) : null)
        ?? (versions.Vogen.Length == 1 ? Unreviewed("Vogen", versions.Vogen[0], [8]) : null);

    static string? NewerThan(string package, string version, int highestReviewedMajor) =>
        MajorOf(version)!.Value > highestReviewedMajor
            ? $"{package} {version} is newer than the highest source-reviewed major ({highestReviewedMajor})"
            : null;

    static string? Unreviewed(string package, string version, int[] reviewedMajors) =>
        reviewedMajors.Contains(MajorOf(version)!.Value)
            ? null
            : $"{package} {version} has no canonical or source-reviewed major-generation evidence";

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
        if (versions.Vogen.Length == 1)
        {
            return versions.IsCritterStack &&
                   (versions.MartenVersion, versions.WolverineVersion, versions.Vogen[0]) == _canonicalVogenCritterStack &&
                   versions.WolverineMarten.Length == 0;
        }

        return versions.Provider == ScreenplayProviders.Marten
            ? _canonicalMarten.Contains(versions.MartenVersion)
            : IsCanonicalCritterStack(versions);
    }

    static bool IsCanonicalCritterStack(PackageVersions versions) =>
        _canonicalCritterStack.Contains((versions.MartenVersion, versions.WolverineVersion!)) &&
        versions.WolverineMarten.Length == 1 &&
        string.Equals(versions.WolverineMarten[0], versions.WolverineVersion, StringComparison.OrdinalIgnoreCase);

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

        public required string[] Marten { get; init; }

        public required string[] Wolverine { get; init; }

        public required string[] WolverineMarten { get; init; }

        public required string[] Vogen { get; init; }

        public required bool HasVogenApiEvidence { get; init; }

        public bool IsCritterStack => Provider == ScreenplayProviders.CritterStack;

        public string MartenVersion => Marten[0];

        public string? WolverineVersion => Wolverine.Length == 0 ? null : Wolverine[0];

        public static PackageVersions From(string provider, LoadedCompilation loaded)
        {
            var packages = loaded.ProjectProvenance.SelectMany(project => project.Packages).ToArray();
            var hasVogenApiEvidence = loaded.ProjectProvenance.Any(project =>
                project.Assemblies.Any(assembly => string.Equals(assembly.Name, "Vogen", StringComparison.OrdinalIgnoreCase)) ||
                project.Capabilities.Contains("vogen.value-object", StringComparer.Ordinal));
            return new PackageVersions
            {
                Provider = provider,
                Marten = VersionsOf(packages, "Marten"),
                Wolverine = VersionsOf(packages, "WolverineFx"),
                WolverineMarten = VersionsOf(packages, "WolverineFx.Marten"),
                Vogen = VersionsOf(packages, "Vogen"),
                HasVogenApiEvidence = hasVogenApiEvidence
            };
        }

        public string Describe()
        {
            var packageSet = Provider == ScreenplayProviders.Marten
                ? $"Marten {Marten[0]}"
                : $"Marten {Marten[0]} with WolverineFx {Wolverine[0]}";
            return Vogen.Length == 1 ? $"{packageSet} and Vogen {Vogen[0]}" : packageSet;
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
