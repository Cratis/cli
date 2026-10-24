// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Cli.Commands.Run;

internal static partial class SampleDataSeeds
{
    static void AddRow(Dictionary<string, Dictionary<string, List<string>>> groups, HashSet<string> authored, string pack, string target, int index, JsonObject row, ReadModelSyntax model, ProjectionSyntax projection, EventSyntax[] events, List<SampleDataSeedWarning> warnings)
    {
        var sourceProperties = projection.Blocks.OfType<FromSyntax>().SelectMany(block => block.Mappings).OfType<SetMappingSyntax>()
            .Where(mapping => mapping.Source is EventSourceIdExpressionSyntax).Select(mapping => mapping.Property).Distinct(StringComparer.Ordinal).ToArray();
        var ids = sourceProperties.Select(property => Value(row, property)).OfType<JsonValue>().Select(value => value.ToString()).Distinct(StringComparer.Ordinal).ToArray();
        if (sourceProperties.Length > 0 && (ids.Length != 1 || string.IsNullOrWhiteSpace(ids[0])))
        {
            Warn(warnings, $"{target} row {index + 1} is not seeded: its event source id is missing or ambiguous.", projection.Location);
            return;
        }

        var id = ids.Length == 1 ? ids[0] : new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{pack}/{target}/{index}"))[..16]).ToString();
        if (authored.Contains(id))
        {
            Warn(warnings, $"{target} row {index + 1} is not seeded for '{id}': the authored seed wins.", projection.Location);
            return;
        }

        var entries = new List<string>();
        var state = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        foreach (var block in projection.Blocks.OfType<FromSyntax>().Where(block => block.Key is null && block.ParentKey is null))
        {
            foreach (var spec in block.Events.Where(spec => spec.Key is null))
            {
                var candidates = events.Where(candidate => candidate.Name == spec.Event.Split('.')[^1]).ToArray();
                if (candidates.Length != 1)
                {
                    Warn(warnings, $"{target} is not seeded: event '{spec.Event}' is missing or ambiguous.", block.Location);
                    return;
                }

                var @event = candidates[0];
                var mappings = ScalarMappings(model, projection, block, @event).ToArray();
                var values = EventValues(row, mappings);
                var matchesConstant = mappings.Any(mapping => mapping.Source is LiteralExpressionSyntax literal && JsonNode.DeepEquals(Value(row, mapping.Property), JsonSerializer.SerializeToNode(literal.Value)));
                if (values.Count == 0 && !matchesConstant) continue;
                var missing = @event.Properties.FirstOrDefault(property => !property.Type.IsOptional && (property.Type.IsCollection || !values.TryGetValue(property.Name, out var supplied) || supplied is null));
                if (missing is not null)
                {
                    var reason = missing.Type.IsCollection
                        ? $"required collection '{@event.Name}.{missing.Name}' cannot be supplied yet (Cratis/Screenplay#1000)"
                        : $"required event property '{@event.Name}.{missing.Name}' cannot be supplied by a scalar mapping";
                    Warn(warnings, $"{target} row {index + 1} is not seeded: {reason}.", missing.Location);
                    return;
                }

                if (values.Values.Any(value => value is JsonArray or JsonObject))
                {
                    Warn(warnings, $"{target} row {index + 1} is not seeded: event '{@event.Name}' needs a non-scalar value.", block.Location);
                    return;
                }

                var text = new StringBuilder().Append("    ").Append(@event.Name).AppendLine();
                foreach (var (property, value) in values) text.Append("      ").Append(property).Append(" = ").Append(Scalar(value)).AppendLine();
                entries.Add(text.ToString());
                foreach (var mapping in mappings)
                {
                    if (mapping.Source is LiteralExpressionSyntax literal) state[mapping.Property] = JsonSerializer.SerializeToNode(literal.Value);
                    else if (mapping.Source is PathExpressionSyntax path && values.TryGetValue(path.Path, out var value)) state[mapping.Property] = value;
                    else if (mapping.Source is EventSourceIdExpressionSyntax) state[mapping.Property] = JsonValue.Create(id);
                }
            }
        }

        if (entries.Count == 0)
        {
            Warn(warnings, $"{target} is not seeded: no event can be derived from its scalar projection mappings.", projection.Location);
            return;
        }

        foreach (var property in model.Properties.Where(property => !property.Type.IsCollection))
        {
            if (TryValue(row, property.Name, out var expected) && (!state.TryGetValue(property.Name, out var actual) || !JsonNode.DeepEquals(expected, actual)))
            {
                Warn(warnings, $"{target}.{property.Name} cannot be reproduced from scalar mappings; only the derivable fields are seeded.", property.Location);
            }
        }

        if (!groups.TryGetValue(pack, out var sources)) groups[pack] = sources = new(StringComparer.Ordinal);
        if (!sources.TryGetValue(id, out var existing)) sources[id] = existing = [];
        foreach (var entry in entries.Where(entry => !existing.Contains(entry, StringComparer.Ordinal))) existing.Add(entry);
    }

    static IEnumerable<SetMappingSyntax> ScalarMappings(ReadModelSyntax model, ProjectionSyntax projection, FromSyntax block, EventSyntax @event)
    {
        var explicitProperties = block.Mappings.Select(mapping => mapping.Property).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (projection.AutoMap != AutoMapMode.Disabled)
        {
            foreach (var property in model.Properties.Where(property => !property.Type.IsCollection && !explicitProperties.Contains(property.Name)))
            {
                var source = @event.Properties.FirstOrDefault(candidate => !candidate.Type.IsCollection && string.Equals(candidate.Name, property.Name, StringComparison.OrdinalIgnoreCase));
                if (source is not null) yield return new(property.Name, new PathExpressionSyntax(source.Name, source.Location), property.Location);
            }
        }

        foreach (var mapping in block.Mappings.OfType<SetMappingSyntax>())
        {
            if (model.Properties.Any(property => !property.Type.IsCollection && string.Equals(property.Name, mapping.Property, StringComparison.OrdinalIgnoreCase))) yield return mapping;
        }
    }

    static Dictionary<string, JsonNode?> EventValues(JsonObject row, IEnumerable<SetMappingSyntax> mappings)
    {
        var result = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in mappings)
        {
            if (mapping.Source is PathExpressionSyntax path && !path.Path.Contains('.', StringComparison.Ordinal) && TryValue(row, mapping.Property, out var value)) result[path.Path] = value;
        }

        return result;
    }

    static JsonNode? Value(JsonObject row, string property) => TryValue(row, property, out var value) ? value : null;

    static bool TryValue(JsonObject row, string property, out JsonNode? value)
    {
        var member = row.FirstOrDefault(member => string.Equals(member.Key, property, StringComparison.OrdinalIgnoreCase));
        value = member.Value;
        return member.Key is not null;
    }

    static string Scalar(JsonNode? value) => value is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? Quote(text) : value?.ToJsonString() ?? "null";
}
