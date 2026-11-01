// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Scene.Engine.Packages;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents a document-set rendering request that preserves an author's identity catalog and stable document keys.
/// </summary>
/// <param name="Documents">The exact source documents and their authoritative identity assignments.</param>
/// <param name="ApplicationName">The application display name, independent of document locations.</param>
/// <param name="Target">The statically bundled renderer target.</param>
/// <param name="ProjectName">The generated project name, defaulting to the application name.</param>
/// <param name="RootNamespace">The requested root namespace, defaulting to the application name.</param>
internal sealed record ScreenplayDocumentRenderRequest(
    SemanticDocumentSet Documents,
    string ApplicationName,
    string Target,
    string? ProjectName = null,
    string? RootNamespace = null)
{
    /// <summary>
    /// Gets the publication destination, or null for destination-independent planning.
    /// </summary>
    public string? Destination { get; init; }

    /// <summary>
    /// Gets whether degradation fails acceptance after safe publication.
    /// </summary>
    public bool Strict { get; init; }

    /// <summary>
    /// Gets whether the scaffold owns a fresh repository root.
    /// </summary>
    public bool RepositoryRoot { get; init; }

    /// <summary>
    /// Gets the authored scope addresses, or an empty array for the application.
    /// </summary>
    public string[] Scope { get; init; } = [];

    /// <summary>
    /// Gets the read models to migrate from a classic reducer to a self-reducing read model, by dotted address or semantic id.
    /// </summary>
    public string[] MigrateReducers { get; init; } = [];

    /// <summary>
    /// Gets the whole-application release-baseline check, when supplied.
    /// </summary>
    public IRenderReleaseBaseline? ReleaseBaseline { get; init; }

    /// <summary>Warnings from the source-file attachment loader.</summary>
    public ImmutableArray<Diagnostic> AttachmentDiagnostics { get; init; } = [];

    /// <summary>Authored Scene composition translated from the same documents, when the source declares screens.</summary>
    public SceneApplication? Scene { get; init; }

    /// <summary>Gets the companion strings files of the model, when it has any.</summary>
    public RenderStringsCatalog? Strings { get; init; }

    /// <summary>Gets the custom runtime packages the application approved, beyond the packages the target ships.</summary>
    public ApprovedPackageSet ApprovedPackages { get; init; } = ApprovedPackageSet.Empty;

    /// <summary>
    /// Gets the plain-text lines every generated code file starts with, or an empty array for no header.
    /// </summary>
    public ImmutableArray<string> FileHeader { get; init; } = [];
}
