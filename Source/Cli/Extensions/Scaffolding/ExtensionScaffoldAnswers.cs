// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// What has been decided so far: a flag the person gave, or an answer the wizard collected. Null means not decided yet.
/// </summary>
/// <param name="Name">The package name.</param>
/// <param name="Kind">The kind of package.</param>
/// <param name="Backend">The backend language, or <c language="text">none</c>.</param>
/// <param name="Frontend">The frontend kind, or <c language="text">none</c>.</param>
/// <param name="Component">The component kind of a frontend-only package.</param>
/// <param name="DisplayName">The name a person sees.</param>
/// <param name="Description">What the package does.</param>
/// <param name="Publisher">The publisher.</param>
public record ExtensionScaffoldAnswers(
    string? Name = null,
    ExtensionScaffoldKind? Kind = null,
    string? Backend = null,
    string? Frontend = null,
    string? Component = null,
    string? DisplayName = null,
    string? Description = null,
    string? Publisher = null)
{
    /// <summary>
    /// Turns the answers into a request, taking the documented default for anything not decided: a full extension, no backend,
    /// no frontend. A frontend-only package has no default component, so validation asks for one.
    /// </summary>
    /// <returns>The request.</returns>
    public ExtensionScaffoldRequest ToRequest() => new(
        Name ?? string.Empty,
        Kind ?? ExtensionScaffoldKind.Extension,
        Backend ?? ExtensionScaffoldVocabulary.None,
        Frontend ?? ExtensionScaffoldVocabulary.None,
        Component ?? ExtensionScaffoldVocabulary.None,
        DisplayName,
        Description,
        Publisher);
}
