// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// A profile offered by the Cratis AI corpus.
/// </summary>
/// <param name="Id">The profile identifier.</param>
/// <param name="Catalog">The public or engineering catalog that offers the profile.</param>
/// <param name="Languages">The languages the profile supports.</param>
/// <param name="Composes">The profiles it composes.</param>
/// <param name="Description">What the profile is for, when the catalog says.</param>
/// <param name="Featured">Whether the catalog recommends the profile as a starting point.</param>
public record AiProfile(string Id, string Catalog, IReadOnlyList<string> Languages, IReadOnlyList<string> Composes, string? Description = null, bool Featured = false);
