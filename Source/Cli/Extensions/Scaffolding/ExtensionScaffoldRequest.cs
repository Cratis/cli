// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// What a person asked <c language="bash">cratis extension new</c> to create, however they said it: flags and the wizard both end here.
/// </summary>
/// <param name="Name">The package name, a PascalCase identifier such as <c language="text">Stripe</c>.</param>
/// <param name="Kind">The kind of package.</param>
/// <param name="Backend">The backend language of a full extension, one of <see cref="ExtensionScaffoldVocabulary.Backends"/>.</param>
/// <param name="Frontend">The frontend kind of a full extension, one of <see cref="ExtensionScaffoldVocabulary.Frontends"/>.</param>
/// <param name="Component">The component kind of a frontend-only package, one of <see cref="ExtensionScaffoldVocabulary.Components"/>.</param>
/// <param name="DisplayName">The name a person sees; defaults to the package name spelled out.</param>
/// <param name="Description">What the package does.</param>
/// <param name="Publisher">The publisher, such as the OCI namespace or npm scope.</param>
public record ExtensionScaffoldRequest(
    string Name,
    ExtensionScaffoldKind Kind,
    string Backend = ExtensionScaffoldVocabulary.None,
    string Frontend = ExtensionScaffoldVocabulary.None,
    string Component = ExtensionScaffoldVocabulary.None,
    string? DisplayName = null,
    string? Description = null,
    string? Publisher = null);
