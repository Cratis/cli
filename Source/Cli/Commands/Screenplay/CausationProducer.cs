// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Observations;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Attributes an appended event to the command or reactor that produced it, from its causation chain.
/// </summary>
/// <remarks>
/// The chain is ordered from the root cause to the append, so the innermost producer is the last one recognized.
/// Only the producer's name is read; command property values and every other causation detail are ignored.
/// </remarks>
public static class CausationProducer
{
    /// <summary>
    /// The causation type Arc records for a command.
    /// </summary>
    public const string CommandCausationType = "Command";

    /// <summary>
    /// The causation property Arc records the command type's short name in.
    /// </summary>
    public const string CommandTypeProperty = "commandType";

    /// <summary>
    /// The causation type the Chronicle client records for a reactor.
    /// </summary>
    public const string ReactorCausationType = "Client Reactor";

    /// <summary>
    /// The causation property the Chronicle client records the reactor identifier in.
    /// </summary>
    public const string ReactorIdProperty = "ReactorId";

    /// <summary>
    /// Finds the innermost recognized producer in a causation chain.
    /// </summary>
    /// <param name="causation">The causation chain, root first.</param>
    /// <returns>The producer, or <see langword="null"/> when the chain names no command or reactor.</returns>
    public static ObservedProducer? From(IEnumerable<Causation>? causation) =>
        (causation ?? []).Select(Recognize).LastOrDefault(producer => producer is not null);

    static ObservedProducer? Recognize(Causation causation)
    {
        var properties = causation.Properties ?? new Dictionary<string, string>();
        if (string.Equals(causation.Type, CommandCausationType, StringComparison.Ordinal) &&
            properties.TryGetValue(CommandTypeProperty, out var command) && !string.IsNullOrWhiteSpace(command))
        {
            return new(ObservedProducerKind.Command, command);
        }

        return string.Equals(causation.Type, ReactorCausationType, StringComparison.Ordinal) &&
            properties.TryGetValue(ReactorIdProperty, out var reactor) && !string.IsNullOrWhiteSpace(reactor)
            ? new(ObservedProducerKind.Reaction, reactor)
            : null;
    }
}
