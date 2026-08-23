// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the evidence tier assigned to a source framework package set.
/// </summary>
public enum ScreenplaySupportTier
{
    /// <summary>
    /// The exact package set and its asserted behaviors pass a pinned canonical fixture.
    /// </summary>
    Canonical,

    /// <summary>
    /// The framework source and metadata names have been reviewed, but this exact package set is not canonical.
    /// </summary>
    SourceReviewed,

    /// <summary>
    /// The APIs are recognized but part of their source semantics cannot be interpreted exactly.
    /// </summary>
    RecognizedWithLoss,

    /// <summary>
    /// The package or API generation is outside available evidence.
    /// </summary>
    Unknown,

    /// <summary>
    /// The adapter deliberately excludes the package or API generation.
    /// </summary>
    Unsupported
}

/// <summary>
/// Represents whether the selected provider recognized the source framework evidence.
/// </summary>
public enum ScreenplayRecognitionStatus
{
    /// <summary>
    /// The provider recognized the required source framework evidence.
    /// </summary>
    Recognized,

    /// <summary>
    /// The available evidence is insufficient to recognize the framework generation safely.
    /// </summary>
    Unknown,

    /// <summary>
    /// The recognized framework generation is deliberately unsupported.
    /// </summary>
    Unsupported
}

/// <summary>
/// Represents what static generation establishes about application semantics.
/// </summary>
public enum ScreenplaySemanticConformance
{
    /// <summary>
    /// Semantic conformance was not evaluated because compatibility admission stopped generation.
    /// </summary>
    NotEvaluated,

    /// <summary>
    /// Static interpretation completed, but human review is still required before downstream use.
    /// </summary>
    RequiresHumanReview,

    /// <summary>
    /// Generation found contradictory or invalid source evidence.
    /// </summary>
    Contradictory,

    /// <summary>
    /// Semantic conformance was not evaluated because the framework generation is unsupported.
    /// </summary>
    Unsupported
}

/// <summary>
/// Represents what the generated Screenplay preserved after source interpretation.
/// </summary>
public enum ScreenplayLoweringFidelity
{
    /// <summary>
    /// Lowering was not evaluated because compatibility admission stopped generation.
    /// </summary>
    NotEvaluated,

    /// <summary>
    /// No lowering loss was reported; this is not a claim of runtime equivalence.
    /// </summary>
    NoReportedLoss,

    /// <summary>
    /// One or more diagnostics report omitted or approximated behavior.
    /// </summary>
    LossReported,

    /// <summary>
    /// Generation failed before a trustworthy Screenplay could be produced.
    /// </summary>
    Failed
}

/// <summary>
/// Represents the resolved NuGet identity of a source framework package.
/// </summary>
/// <param name="Id">The package identifier.</param>
/// <param name="Version">The resolved package version.</param>
public record ResolvedScreenplayPackage(string Id, string Version);

/// <summary>
/// Represents a framework assembly that corroborates resolved package provenance.
/// </summary>
/// <param name="Name">The assembly name.</param>
/// <param name="Version">The assembly version.</param>
public record ScreenplayAssemblyIdentity(string Name, string Version);

/// <summary>
/// Represents source-framework provenance for one selected project and target framework.
/// </summary>
/// <param name="Project">The project name.</param>
/// <param name="TargetFramework">The target framework selected by the workspace.</param>
/// <param name="Packages">The resolved source-framework packages for the selected target.</param>
/// <param name="Assemblies">The referenced framework assemblies that corroborate the packages.</param>
/// <param name="Capabilities">The recognized assembly capability fingerprints.</param>
public record ScreenplayProjectProvenance(
    string Project,
    string? TargetFramework,
    IReadOnlyList<ResolvedScreenplayPackage> Packages,
    IReadOnlyList<ScreenplayAssemblyIdentity> Assemblies,
    IReadOnlyList<string> Capabilities)
{
    /// <summary>
    /// Gets the relocation-safe source-path policy used for this project, when supplied by the workspace host.
    /// </summary>
    public ScreenplaySourcePolicyProvenance? SourcePolicy { get; init; }
}

/// <summary>
/// Represents compatibility evidence separately from recognition, semantic review, and lowering fidelity.
/// </summary>
/// <param name="SupportTier">The package-set evidence tier.</param>
/// <param name="RecognitionStatus">Whether the provider recognized the framework generation.</param>
/// <param name="SemanticConformance">What static interpretation established about application semantics.</param>
/// <param name="LoweringFidelity">What Screenplay lowering reported.</param>
/// <param name="Explanation">Why the tier and statuses were assigned.</param>
public record ScreenplayCompatibilityReport(
    ScreenplaySupportTier SupportTier,
    ScreenplayRecognitionStatus RecognitionStatus,
    ScreenplaySemanticConformance SemanticConformance,
    ScreenplayLoweringFidelity LoweringFidelity,
    string Explanation);

/// <summary>
/// Represents the complete CLI-owned provenance of one source-to-Screenplay generation.
/// </summary>
/// <param name="Provider">The selected source provider.</param>
/// <param name="ProviderVersion">The bundled provider package version, or its assembly version when package metadata is unavailable.</param>
/// <param name="Projects">The selected project, target-framework, package, assembly, and capability evidence.</param>
/// <param name="Compatibility">The compatibility assessment when the provider publishes one.</param>
public record ScreenplayGenerationProvenance(
    string Provider,
    string ProviderVersion,
    IReadOnlyList<ScreenplayProjectProvenance> Projects,
    ScreenplayCompatibilityReport? Compatibility);
