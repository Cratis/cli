// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Describes how one contract differs from another by what each offers: its abilities, triggers and components, and their typed shapes.
/// </summary>
/// <remarks>
/// The shape of a member is its property names and types only. Where the members sit in the contract file does not matter, so moving
/// lines in a newer contract is not a change.
/// </remarks>
public static class ExtensionContractDiff
{
    /// <summary>
    /// Lists what <paramref name="previous"/> offered that <paramref name="next"/> no longer offers, or offers with another shape.
    /// </summary>
    /// <param name="previous">The contract the model was written against.</param>
    /// <param name="next">The contract that replaces it.</param>
    /// <returns>One message per member.</returns>
    public static string[] Removed(ExtensionContract previous, ExtensionContract next)
    {
        var found = new List<string>();
        foreach (var ability in previous.Abilities)
        {
            var other = next.Abilities.FirstOrDefault(candidate => candidate.Name == ability.Name);
            if (other is null) found.Add($"ability '{ability.Name}' is not offered");
            else if (Shape(ability.Inputs) != Shape(other.Inputs) || Shape(ability.Returns) != Shape(other.Returns)) found.Add($"ability '{ability.Name}' has different inputs or returns");
        }

        foreach (var trigger in previous.Triggers)
        {
            var other = next.Triggers.FirstOrDefault(candidate => candidate.Name == trigger.Name);
            if (other is null) found.Add($"trigger '{trigger.Name}' is not offered");
            else if (Shape(trigger.Payload) != Shape(other.Payload)) found.Add($"trigger '{trigger.Name}' has a different payload");
        }

        foreach (var component in previous.Components)
        {
            var other = next.Components.FirstOrDefault(candidate => candidate.Name == component.Name);
            if (other is null)
            {
                found.Add($"component '{component.Name}' is not offered");
                continue;
            }

            if (Shape(component.Properties) != Shape(other.Properties)) found.Add($"component '{component.Name}' has different properties");
            foreach (var signal in component.Signals)
            {
                var raised = other.Signals.FirstOrDefault(candidate => candidate.Name == signal.Name);
                if (raised is null) found.Add($"component '{component.Name}' does not raise '{signal.Name}'");
                else if (Shape(signal.Values) != Shape(raised.Values)) found.Add($"component '{component.Name}' raises '{signal.Name}' with different values");
            }
        }

        return [.. found];
    }

    /// <summary>
    /// Lists what <paramref name="next"/> offers that <paramref name="previous"/> did not.
    /// </summary>
    /// <param name="previous">The contract the model was written against.</param>
    /// <param name="next">The contract that replaces it.</param>
    /// <returns>One message per member.</returns>
    public static string[] Added(ExtensionContract previous, ExtensionContract next) =>
    [
        .. next.Abilities.Where(ability => previous.Abilities.All(candidate => candidate.Name != ability.Name)).Select(ability => $"ability '{ability.Name}' is new"),
        .. next.Triggers.Where(trigger => previous.Triggers.All(candidate => candidate.Name != trigger.Name)).Select(trigger => $"trigger '{trigger.Name}' is new"),
        .. next.Components.Where(component => previous.Components.All(candidate => candidate.Name != component.Name)).Select(component => $"component '{component.Name}' is new")
    ];

    static string Shape(IEnumerable<PropertySyntax> properties) =>
        string.Join(';', properties.OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => $"{property.Name}:{property.Type.Name}{(property.Type.IsCollection ? "[]" : string.Empty)}{(property.Type.IsOptional ? "?" : string.Empty)}"));
}
