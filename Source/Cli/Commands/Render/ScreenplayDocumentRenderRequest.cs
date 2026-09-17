// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

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
    string? RootNamespace = null);
