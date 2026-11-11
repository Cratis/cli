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
    static void AddRow(Dictionary<string, Dictionary<string, List<string>>> groups, HashSet<string> authored, RowContext context)
    {
        if (!TryResolveId(context, authored, out var id)) return;
        if (!TryDeriveEntries(context, id, out var draft)) return;
        if (draft.Entries.Count == 0)
        {
            Warn(context.Warnings, $"{context.Target} is not seeded: no event can be derived from its scalar projection mappings.", context.Projection.Location);
            return;
        }

        WarnUnreproduced(context, draft.State);
        Merge(groups, context.Pack, id, draft.Entries);
    }

    static bool TryResolveId(RowContext context, HashSet<string> authored, out string id)
    {
        id = string.Empty;
        var sourceProperties = context.Projection.Blocks.OfType<FromSyntax>().SelectMany(block => block.Mappings).OfType<SetMappingSyntax>()
            .Where(mapping => mapping.Source is EventSourceIdExpressionSyntax).Select(mapping => mapping.Property).Distinct(StringComparer.Ordinal).ToArray();
        var ids = sourceProperties.Select(property => Value(context.Row, property)).OfType<JsonValue>().Select(value => value.ToString()).Distinct(StringComparer.Ordinal).ToArray();
        if (sourceProperties.Length > 0 && IsMissingOrAmbiguous(ids))
        {
            Warn(context.Warnings, $"{context.Target} row {context.Index + 1} is not seeded: its event source id is missing or ambiguous.", context.Projection.Location);
            return false;
        }

        id = ids.Length == 1 ? ids[0] : new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{context.Pack}/{context.Target}/{context.Index}"))[..16]).ToString();
        if (!authored.Contains(id)) return true;
        Warn(context.Warnings, $"{context.Target} row {context.Index + 1} is not seeded for '{id}': the authored seed wins.", context.Projection.Location);
        return false;
    }

    static bool IsMissingOrAmbiguous(string[] ids) => ids.Length != 1 || string.IsNullOrWhiteSpace(ids[0]);

    static bool TryDeriveEntries(RowContext context, string id, out SeedDraft draft)
    {
        draft = new(id, [], [with(StringComparer.OrdinalIgnoreCase)]);
        foreach (var block in context.Projection.Blocks.OfType<FromSyntax>().Where(block => block.Key is null && block.ParentKey is null))
        {
            foreach (var eventName in block.Events.Where(spec => spec.Key is null).Select(spec => spec.Event))
            {
                if (!TryDeriveEvent(context, block, eventName, draft)) return false;
            }
        }

        return true;
    }

    static bool TryDeriveEvent(RowContext context, FromSyntax block, string eventName, SeedDraft draft)
    {
        var candidates = context.Events.Where(candidate => candidate.Name == eventName.Split('.')[^1]).ToArray();
        if (candidates.Length != 1)
        {
            Warn(context.Warnings, $"{context.Target} is not seeded: event '{eventName}' is missing or ambiguous.", block.Location);
            return false;
        }

        var @event = candidates[0];
        var mappings = ScalarMappings(context.Model, context.Projection, block, @event).ToArray();
        var values = EventValues(context.Row, mappings);
        if (!DerivesFromRow(context.Row, mappings, values)) return true;
        if (!IsSeedable(context, block, @event, values)) return false;

        var text = new StringBuilder().Append("    ").AppendLine(@event.Name);
        foreach (var (property, value) in values) text.Append("      ").Append(property).Append(" = ").AppendLine(Scalar(value));
        draft.Entries.Add(text.ToString());
        foreach (var mapping in mappings) ApplyToState(draft, mapping, values);
        return true;
    }

    static bool DerivesFromRow(JsonObject row, SetMappingSyntax[] mappings, Dictionary<string, JsonNode?> values)
    {
        if (values.Count > 0) return true;
        var matchesConstant = mappings.Any(mapping => mapping.Source is LiteralExpressionSyntax literal && JsonNode.DeepEquals(Value(row, mapping.Property), JsonSerializer.SerializeToNode(literal.Value)));
        var hasEventSourceId = mappings.Any(mapping => mapping.Source is EventSourceIdExpressionSyntax && Value(row, mapping.Property) is JsonValue);
        return matchesConstant || hasEventSourceId;
    }

    static bool IsSeedable(RowContext context, FromSyntax block, EventSyntax @event, Dictionary<string, JsonNode?> values)
    {
        var missing = @event.Properties.FirstOrDefault(property => !property.Type.IsOptional && (property.Type.IsCollection || !values.TryGetValue(property.Name, out var supplied) || supplied is null));
        if (missing is not null)
        {
            var reason = missing.Type.IsCollection
                ? $"required collection '{@event.Name}.{missing.Name}' cannot be supplied yet (Cratis/Screenplay#1000)"
                : $"required event property '{@event.Name}.{missing.Name}' cannot be supplied by a scalar mapping";
            Warn(context.Warnings, $"{context.Target} row {context.Index + 1} is not seeded: {reason}.", missing.Location);
            return false;
        }

        if (!values.Values.Any(value => value is JsonArray or JsonObject)) return true;
        Warn(context.Warnings, $"{context.Target} row {context.Index + 1} is not seeded: event '{@event.Name}' needs a non-scalar value.", block.Location);
        return false;
    }

    static void ApplyToState(SeedDraft draft, SetMappingSyntax mapping, Dictionary<string, JsonNode?> values)
    {
        var state = draft.State;
        if (mapping.Source is LiteralExpressionSyntax literal) state[mapping.Property] = JsonSerializer.SerializeToNode(literal.Value);
        else if (mapping.Source is PathExpressionSyntax path && values.TryGetValue(path.Path, out var value)) state[mapping.Property] = value;
        else if (mapping.Source is EventSourceIdExpressionSyntax) state[mapping.Property] = JsonValue.Create(draft.Id);
    }

    static void WarnUnreproduced(RowContext context, Dictionary<string, JsonNode?> state)
    {
        foreach (var property in context.Model.Properties.Where(property => !property.Type.IsCollection))
        {
            if (TryValue(context.Row, property.Name, out var expected) && !Reproduced(state, property.Name, expected))
            {
                Warn(context.Warnings, $"{context.Target}.{property.Name} cannot be reproduced from scalar mappings; only the derivable fields are seeded.", property.Location);
            }
        }
    }

    static bool Reproduced(Dictionary<string, JsonNode?> state, string property, JsonNode? expected) => state.TryGetValue(property, out var actual) && JsonNode.DeepEquals(expected, actual);

    static void Merge(Dictionary<string, Dictionary<string, List<string>>> groups, string pack, string id, List<string> entries)
    {
        if (!groups.TryGetValue(pack, out var sources)) groups[pack] = sources = [with(StringComparer.Ordinal)];
        if (!sources.TryGetValue(id, out var existing)) sources[id] = existing = [];
        existing.AddRange(entries.Where(entry => !existing.Contains(entry, StringComparer.Ordinal)));
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
            if (TryDirectPath(mapping, out var path) && TryValue(row, mapping.Property, out var value)) result[path] = value;
        }

        return result;
    }

    static bool TryDirectPath(SetMappingSyntax mapping, out string path)
    {
        path = mapping.Source is PathExpressionSyntax source ? source.Path : string.Empty;
        return mapping.Source is PathExpressionSyntax && !path.Contains('.', StringComparison.Ordinal);
    }

    static JsonNode? Value(JsonObject row, string property) => TryValue(row, property, out var value) ? value : null;

    static bool TryValue(JsonObject row, string property, out JsonNode? value)
    {
        var member = row.FirstOrDefault(member => string.Equals(member.Key, property, StringComparison.OrdinalIgnoreCase));
        value = member.Value;
        return member.Key is not null;
    }

    static string Scalar(JsonNode? value) => value is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? Quote(text) : value?.ToJsonString() ?? "null";

    sealed record SeedDraft(string Id, List<string> Entries, Dictionary<string, JsonNode?> State);

    sealed record RowContext(string Pack, string Target, int Index, JsonObject Row, ReadModelSyntax Model, ProjectionSyntax Projection, EventSyntax[] Events, List<SampleDataSeedWarning> Warnings);
}
