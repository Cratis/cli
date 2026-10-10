// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// A CLI policy classification of a typed structural change, not a behavioral equivalence claim.
/// </summary>
/// <param name="Category">MissingFromModel, NotRealizedInCode, ShapeMismatch or Informational.</param>
/// <param name="Kind">The compared declaration kind.</param>
/// <param name="Address">The full compared declaration address.</param>
/// <param name="Change">The typed change name.</param>
/// <param name="Member">The changed member or event property, if any.</param>
/// <param name="BeforeType">The authored event property type, if available.</param>
/// <param name="AfterType">The recovered event property type, if available.</param>
/// <param name="Blocking">Whether this finding fails the conformance check.</param>
/// <param name="SameNameCounterpart">Unmatched opposite declaration with the same kind and last name segment; a hint only.</param>
public sealed record ConformanceFinding(string Category, string Kind, string Address, string Change, string? Member, string? BeforeType, string? AfterType, bool Blocking, string? SameNameCounterpart = null);
