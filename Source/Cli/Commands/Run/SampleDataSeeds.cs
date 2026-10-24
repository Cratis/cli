// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Scene.Engine.SampleData;
using Cratis.Scene.Model.SampleData;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.Commands.Run;

internal static partial class SampleDataSeeds
{
    internal static (string? SeedPlay, IReadOnlyList<SampleDataSeedWarning> Warnings, string? Error) Create(ApplicationSyntax application, SceneApplication scene, string? packId)
    {
        var warnings = new List<SampleDataSeedWarning>();
        var packs = scene.SampleDataPacks ?? [];
        var blueprints = scene.BlueprintSampleDataPacks ?? [];
        if (packId is not null && !packs.Any(pack => pack.Id == packId) && !blueprints.Contains(packId, StringComparer.Ordinal))
        {
            var normalized = SampleDataConverter.IdOf(packId);
            if (packs.Any(pack => pack.Id == normalized)) packId = normalized;
        }

        if (packId is not null && !packs.Any(pack => pack.Id == packId) && !blueprints.Contains(packId, StringComparer.Ordinal))
        {
            return (null, warnings, $"Sample data pack '{packId}' was not found.");
        }

        var profile = scene.UiProfiles.FirstOrDefault();
        var selection = profile?.SampleData;
        if (packId is not null && profile is not null)
        {
            profile = profile with { SampleData = new(selection?.Mappings ?? [], [], packId) };
        }

        var active = packId ?? selection?.ActivePack;
        IEnumerable<string?> selected = packId is null ? (selection?.Bindings ?? []).Select(binding => binding.Pack).Append(active) : [packId];
        foreach (var blueprint in selected.Where(id => id is not null && blueprints.Contains(id, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal))
        {
            Warn(warnings, $"Blueprint sample data pack '{blueprint}' is not seeded yet; only user-authored packs are available to cratis run.", null);
        }

        var context = new SampleDataContext(packs, profile, SessionActivePack: active, IsDesignOrPreview: true);
        var features = application.Modules.SelectMany(module => Features(module.Features, module.Name)).ToArray();
        var slices = features.SelectMany(feature => feature.Feature.Slices.Select(slice => (feature.Path, Slice: slice))).ToArray();
        var events = slices.SelectMany(slice => slice.Slice.Events).ToArray();
        var projections = slices.SelectMany(slice => slice.Slice.Projections.Select(projection => (slice.Path, Projection: projection))).ToArray();
        var authored = application.Seeds.SelectMany(seed => seed.Groups).Select(source => source.EventSourceId).ToHashSet(StringComparer.Ordinal);
        var groups = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.Ordinal);
        var bindings = profile?.SampleData?.Bindings ?? [];
        var models = features.SelectMany(feature => (feature.Feature.ReadModels ?? [])
            .Concat(feature.Feature.Slices.SelectMany(slice => slice.ReadModels ?? []))
            .Select(model => (feature.Path, Model: model)));
        foreach (var (path, model) in models)
        {
            var target = $"{path}.{model.Name}";
            var requested = bindings.Any(binding => binding.Target == model.Name) && !bindings.Any(binding => binding.Target == target) ? model.Name : target;
            var rows = SampleDataResolver.Resolve(new(requested), context);
            if (rows.Pack is null) rows = SampleDataResolver.Resolve(new(model.Name), context);
            if (rows.Pack is null || rows.Rows.Count == 0) continue;
            var candidates = projections.Where(candidate => candidate.Projection.ReadModel == target ||
                (candidate.Projection.ReadModel == model.Name && candidate.Path == path)).ToArray();
            if (candidates.Length != 1)
            {
                Warn(warnings, $"{target} is not seeded: it needs one unambiguous projection.", model.Location);
                continue;
            }

            var projection = candidates[0].Projection;
            WarnUnsupported(model, projection, target, warnings);
            if (projection.Key is not null) continue;
            for (var index = 0; index < rows.Rows.Count; index++)
            {
                AddRow(groups, authored, rows.Pack, target, index, rows.Rows[index], model, projection, events, warnings);
            }
        }

        var text = new StringBuilder();
        foreach (var (pack, sources) in groups)
        {
            text.Append("seed ").Append(Quote($"Sample data {pack}")).AppendLine();
            foreach (var (id, entries) in sources)
            {
                text.Append("  for ").Append(Quote(id)).AppendLine();
                foreach (var entry in entries) text.Append(entry);
            }
        }

        return (text.Length == 0 ? null : text.ToString(), warnings, null);
    }

    static IEnumerable<(string Path, FeatureSyntax Feature)> Features(IEnumerable<FeatureSyntax> features, string parent)
    {
        foreach (var feature in features)
        {
            var path = $"{parent}.{feature.Name}";
            yield return (path, feature);
            foreach (var nested in Features(feature.Features, path)) yield return nested;
        }
    }

    static void Warn(List<SampleDataSeedWarning> warnings, string message, Cratis.Screenplay.Diagnostics.SourceLocation? location)
    {
        if (!warnings.Any(warning => warning.Message == message)) warnings.Add(new(message, location));
    }

    static string Quote(string value) => $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal).Replace("\t", "\\t", StringComparison.Ordinal)}\"";
}
