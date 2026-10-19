// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Maps model reactions to registered Chronicle reactors where the match is unambiguous.
/// </summary>
/// <remarks>
/// A reactor matches a reaction when its identifier, or the last segment of it, equals the reaction's name. A mapping is
/// made only when the reaction name is unique in the model and exactly one application reactor matches it; anything
/// else stays unmapped, which the comparison reports as Unknown rather than guessing.
/// </remarks>
public static class ReactionIdentities
{
    static readonly char[] _separators = ['.', '+'];

    /// <summary>
    /// Adds the unambiguous reaction mappings to an identity map.
    /// </summary>
    /// <param name="map">The identity map derived from the model.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="consumers">The registered consumers.</param>
    /// <returns>The identity map with reaction producer and consumer identifiers added.</returns>
    public static ObservationIdentityMap Map(ObservationIdentityMap map, ExecutableSemanticModel model, IEnumerable<ObservedConsumer> consumers)
    {
        var reactors = consumers.Where(consumer => consumer.Kind == ObservedConsumerKind.Reaction && consumer.Owner == ObservedConsumerOwner.Application)
            .Select(consumer => consumer.Identifier).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var reaction in Reactions(model.Application).GroupBy(reaction => reaction.Name, StringComparer.Ordinal).Where(group => group.Count() == 1).Select(group => group.Single()))
        {
            var matches = reactors.Where(reactor => Matches(reactor, reaction.Name)).ToArray();
            if (matches.Length == 1)
            {
                map = map.With(reaction.Id, producer: new(ObservedProducerKind.Reaction, matches[0]), consumerIdentifier: matches[0]);
            }
        }

        return map;
    }

    static bool Matches(string reactor, string name) =>
        string.Equals(reactor, name, StringComparison.Ordinal) ||
        string.Equals(reactor[(reactor.LastIndexOfAny(_separators) + 1)..], name, StringComparison.Ordinal);

    static IEnumerable<SemanticReaction> Reactions(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(FeatureReactions));

    static IEnumerable<SemanticReaction> FeatureReactions(SemanticFeature feature) =>
        feature.Slices.SelectMany(slice => slice.Reactions).Concat(feature.Features.SelectMany(FeatureReactions));
}
