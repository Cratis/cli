// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Composes what an agent is asked: the kit's prompt filled from the order, followed by the host's rules for the outcome.
/// A prompt is made only of the kit and the order, which carries no credential, so no credential can reach it.
/// </summary>
public static class BuildPrompts
{
    /// <summary>
    /// The prompt used without a kit.
    /// </summary>
    public const string Default =
        "Build the {part} of {slices} in the feature {featurePath} of {application}, as described by the build order at {order}. " +
        "Read the Screenplay documents and the specifications in the order; every specification is an acceptance criterion.";

    /// <summary>
    /// Composes the prompt for an order.
    /// </summary>
    /// <param name="template">The kit's prompt template, or <see langword="null"/> for <see cref="Default"/>.</param>
    /// <param name="values">The placeholder values for the order.</param>
    /// <returns>The prompt.</returns>
    public static string ForOrder(string? template, IReadOnlyDictionary<string, string> values) =>
        $"{BuildKitPlaceholders.Fill(template ?? Default, values).Trim()}\n\n{OutcomeRules(values["outcome"])}";

    /// <summary>
    /// Composes the prompt that learns an existing codebase's conventions once.
    /// </summary>
    /// <param name="template">The kit's learn prompt.</param>
    /// <param name="values">The placeholder values.</param>
    /// <param name="conventions">The project path the conventions are recorded at.</param>
    /// <returns>The prompt.</returns>
    public static string ForLearning(string template, IReadOnlyDictionary<string, string> values, string conventions) =>
        $"{BuildKitPlaceholders.Fill(template, values).Trim()}\n\nRecord the conventions at {conventions}, relative to the project. Change nothing else.";

    /// <summary>
    /// Asks an agent that stopped without writing its outcome to go on: an agent such as Copilot CLI ends its turn when one
    /// action is refused, as if a person had said no and would answer next.
    /// </summary>
    /// <param name="outcomePath">Where the outcome is written.</param>
    /// <returns>The prompt.</returns>
    public static string ToContinue(string outcomePath) =>
        "You stopped before the build was done: there is no outcome yet. Nobody is watching this session, so nobody will answer. " +
        "An action you asked for was refused because the build kit does not permit it; do not ask for it again, and run one permitted command at a time instead of chaining commands. " +
        $"Go on with the build using what you are permitted, then write the outcome to {outcomePath} with your file editing tool.\n\n{OutcomeRules(outcomePath)}";

    /// <summary>
    /// The host's rules for the outcome document.
    /// </summary>
    /// <param name="outcomePath">Where the outcome is written.</param>
    /// <returns>The rules.</returns>
    public static string OutcomeRules(string outcomePath) =>
        $"When you are done, write the outcome as JSON to {outcomePath}, and nothing else there. It is one of:\n" +
        "- {\"kind\": \"built\", \"files\": [the files you wrote, relative to the project], \"specifications\": [{\"id\": \"<specification id from the order>\", \"result\": \"passed\" | \"failed\" | \"notRun\"}], \"assumptions\": [what you decided that the model does not say]}\n" +
        "- {\"kind\": \"blocked\", \"reason\": \"why the slices cannot be built\", \"questions\": [what needs an answer]}\n" +
        "- {\"kind\": \"nothingToDo\"}\n" +
        "Leave out commit, branch and pull request: the build host verifies the work, commits it and reports it. " +
        "Do not commit, push or change branches yourself, and never write a credential into a file or the outcome.";
}
