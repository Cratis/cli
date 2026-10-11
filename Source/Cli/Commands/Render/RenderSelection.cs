// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render;

internal sealed record RenderScope(string Address, string Kind);

/// <summary>
/// Resolves authored addresses and describes the union requested from Stage.
/// </summary>
internal sealed record RenderSelection(ImmutableArray<ArtifactRenderScope> Scopes, IReadOnlyList<RenderScope> Receipt)
{
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
        var candidates = index.Sources.Where(source => source.Kind is "module" or "feature" or "slice").ToArray();
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
                var segments = path.Split('/');
                var parent = string.Empty;
                var missing = segments[0];
                foreach (var segment in segments)
                {
                    var current = parent.Length == 0 ? segment : $"{parent}/{segment}";
                    if (!candidates.Any(source => source.Address == current))
                    {
                        missing = segment;
                        break;
                    }
                    parent = current;
                }
                var prefix = parent.Length == 0 ? string.Empty : $"{parent}/";
                var siblings = candidates.Select(source => source.Address!)
                    .Where(candidate => candidate.StartsWith(prefix, StringComparison.Ordinal) && !candidate[prefix.Length..].Contains('/'))
                    .Select(candidate => candidate[prefix.Length..]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
                diagnostics.Add(new(ScreenplayDiagnosticSeverity.Error, "CLI-RENDER-011", $"Unknown scope name '{missing}' in '{address}'. Known siblings: {string.Join(", ", siblings.Select(name => $"'{name}'"))}.", address));
            }
        }

        return scopes.Count == receipt.Count && receipt.Count == addresses.Distinct(StringComparer.Ordinal).Count()
            ? new([.. scopes], receipt)
            : null;
    }
}
