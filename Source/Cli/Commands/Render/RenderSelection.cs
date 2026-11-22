// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render;

internal sealed record RenderScope(string Address, string Kind);

/// <summary>
/// Resolves authored addresses and describes the union requested from Stage.
/// </summary>
/// <param name="Scopes">The semantic scopes whose slices Stage unions.</param>
/// <param name="Receipt">The resolved authored addresses and kinds.</param>
internal sealed record RenderSelection(ImmutableArray<ArtifactRenderScope> Scopes, IReadOnlyList<RenderScope> Receipt)
{
    static readonly string[] _scopeKinds = ["module", "feature", "slice"];

    public ImmutableArray<SemanticExclusion> SelectExclusions(ImmutableArray<SemanticExclusion> exclusions, SemanticAddressIndex index)
    {
        if (exclusions.IsDefaultOrEmpty)
        {
            return [];
        }

        var addresses = Scopes.Select(scope => index.Resolve(scope.Artifact.ToString()).Address).OfType<string>().ToArray();
        return [.. exclusions.Where(exclusion => IsInSubtree(exclusion, addresses))];
    }

    public bool Contains(string id, SemanticAddressIndex index) => Scopes.Any(scope =>
        scope.Artifact.ToString() == id || index.Owners(id).Contains(scope.Artifact.ToString(), StringComparer.Ordinal));

    public IReadOnlyList<string> OutsideScope(SemanticAddressIndex index) =>
        [.. index.Sources.Where(source => source.Kind == "slice" && !Contains(source.Id, index))
            .Select(source => source.Address!.Replace('/', '.')).Order(StringComparer.Ordinal)];

    public static RenderSelection? Resolve(string[] addresses, SemanticAddressIndex index, ICollection<ScreenplayDiagnostic> diagnostics)
    {
        if (addresses.Length == 0)
        {
            return null;
        }

        var scopes = new List<ArtifactRenderScope>();
        var receipt = new List<RenderScope>();
        var candidates = index.Sources.Where(source => _scopeKinds.Contains(source.Kind, StringComparer.Ordinal)).ToArray();
        foreach (var address in addresses.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var path = address.Replace('.', '/');
            var matches = candidates.Where(source => source.Address == path).ToArray();
            if (matches.Length == 1)
            {
                var source = matches[0];
                var kind = source.Kind switch
                {
                    "module" => ArtifactRenderScopeKind.Module,
                    "feature" => ArtifactRenderScopeKind.Feature,
                    _ => ArtifactRenderScopeKind.Slice
                };
                scopes.Add(new(kind, index.Identity(source.Id)));
                receipt.Add(new(address, source.Kind));
            }
            else if (matches.Length > 1)
            {
                diagnostics.Add(new(ScreenplayDiagnosticSeverity.Error, "CLI-RENDER-012", $"Scope '{address}' names both a feature and a slice. Choose an unambiguous address.", address));
            }
            else
            {
                diagnostics.Add(UnknownScope(address, path, [.. candidates.Select(source => source.Address!)]));
            }
        }

        return scopes.Count == receipt.Count && receipt.Count == addresses.Distinct(StringComparer.Ordinal).Count()
            ? new([.. scopes], receipt)
            : null;
    }

    static bool IsInSubtree(SemanticExclusion exclusion, string[] addresses) =>
        exclusion.Kind is SemanticExclusionKind.Slice or SemanticExclusionKind.Specification &&
        addresses.Any(address => exclusion.Address.Equals(address, StringComparison.Ordinal) ||
            exclusion.Address.StartsWith($"{address}/", StringComparison.Ordinal));

    static ScreenplayDiagnostic UnknownScope(string address, string path, string[] candidates)
    {
        var (parent, missing) = MissingSegment(path, candidates);
        var prefix = parent.Length == 0 ? string.Empty : $"{parent}/";
        var siblings = candidates
            .Where(candidate => candidate.StartsWith(prefix, StringComparison.Ordinal) && !candidate[prefix.Length..].Contains('/'))
            .Select(candidate => candidate[prefix.Length..]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return new(ScreenplayDiagnosticSeverity.Error, "CLI-RENDER-011", $"Unknown scope name '{missing}' in '{address}'. Known siblings: {string.Join(", ", siblings.Select(name => $"'{name}'"))}.", address);
    }

    static (string Parent, string Missing) MissingSegment(string path, string[] candidates)
    {
        var segments = path.Split('/');
        var parent = string.Empty;
        foreach (var segment in segments)
        {
            var current = parent.Length == 0 ? segment : $"{parent}/{segment}";
            if (!candidates.Contains(current, StringComparer.Ordinal))
            {
                return (parent, segment);
            }
            parent = current;
        }
        return (parent, segments[0]);
    }
}
