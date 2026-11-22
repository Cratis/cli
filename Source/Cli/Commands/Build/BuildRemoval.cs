// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// What a builder is told about slices Studio removed from the model. The host hands over the removal metadata (the slice
/// identifiers, their former names and the declaration names) and nothing more: it never guesses a file path from a
/// declaration name, never invents file lists and never deletes anything itself. The agent or generator resolves, from what
/// it built, the files that belong to those slices and removes them; when it cannot tell which files those are, it says the
/// removal is blocked.
/// </summary>
public static class BuildRemoval
{
    /// <summary>
    /// Why a builder that cannot resolve the files built for a slice from the removal metadata does not remove anything.
    /// </summary>
    public const string UnresolvableReason =
        "This builder has no agent or generator that can tell which files were built for the removed slices, and the order names slices and declarations, not files. Nothing was removed.";

    const string Rules =
        "- The order names the removed slices by identifier, former name and declaration names. Those are names, not file paths: " +
        "never delete a path only because it matches a name, and never delete a folder you have not checked file by file.\n" +
        "- Resolve the files yourself, from what you built or find in the project for exactly these slices: their backend and user interface artifacts, " +
        "specifications and generated proxies. Check the project's conventions and its Git history when they help.\n" +
        "- Delete a file only when it belongs to the removed slices alone. A file shared with other slices stays, and you edit out only what belonged to the removed slices. " +
        "Never touch another slice, and never follow a symbolic link out of the project or write outside it.\n" +
        "- Removing something that is already gone is fine; it is not a failure.\n";

    /// <summary>
    /// Spells out the instructions of a removal-only order, to be read before anything else in the prompt.
    /// </summary>
    /// <param name="order">The removal order.</param>
    /// <returns>The instructions.</returns>
    public static string Instructions(BuildOrder order) =>
        "This order only removes. Nothing is built: ignore any instruction below to build, render or implement slices, and add nothing. " +
        $"Remove what was built for these slices, which are no longer in the model of {order.Application}:\n{Describe(order)}\n\n{Rules}" +
        "- If you cannot tell with confidence which files belong to a removed slice, remove nothing for it and report the removal as blocked, with a question that names what you could not resolve.\n";

    /// <summary>
    /// Spells out the cleanup an ordinary build also does for slices removed from the model since the last build.
    /// </summary>
    /// <param name="order">The order, which carries removed changes beside the slices to build.</param>
    /// <returns>The guidance, or an empty string when the order removes nothing.</returns>
    public static string CleanupGuidance(BuildOrder order) =>
        order.RemovedChanges.Count == 0
            ? string.Empty
            : $"Slices removed from the model (changes), whose files you also clean up:\n{Describe(order)}\n{Rules}" +
              "- List every file you deleted or edited for them in the outcome's files, beside the files you wrote.\n" +
              "- If you cannot tell which files belong to a removed slice, leave them and say so in an assumption.";

    /// <summary>
    /// Spells out what the outcome of a removal says, after the host's general rules for the outcome.
    /// </summary>
    /// <returns>The rules.</returns>
    public static string OutcomeRules() =>
        "\nThis order removes: in a built outcome, files lists every path you deleted or edited, as it was relative to the project, including files that no longer exist. " +
        "Report nothingToDo only when every file that belonged to the removed slices was already gone. " +
        "Report blocked, and not nothingToDo, when you could not resolve which files belong to them.";

    /// <summary>
    /// Checks the outcome of a removal order before the host acts on it: a removal that built must say what it deleted.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="outcome">The outcome read from the agent.</param>
    /// <param name="project">The project folder the reported paths are relative to.</param>
    /// <returns>The outcome, or a retryable failure when it claims the removal without naming a single path, or names a folder instead of the files.</returns>
    public static JsonObject Check(BuildOrder order, JsonObject outcome, string project)
    {
        if (!order.IsRemoval || outcome["kind"]?.GetValue<string>() != BuildOutcomes.Built) return outcome;
        if (outcome["files"] is not JsonArray { Count: > 0 } files) return BuildOutcomes.Failure(order, "The removal was reported as built without naming the files it removed.", true);
        return files.Select(file => file?.ToString() ?? string.Empty).Any(file => NamesFolder(project, file))
            ? BuildOutcomes.Failure(order, "The removal named a folder. List every file it removed or edited, never a folder.", true)
            : outcome;
    }

    static bool NamesFolder(string project, string file) =>
        file.Length == 0 || file.EndsWith('/') || string.Equals(file, ".", StringComparison.Ordinal) || string.Equals(file, "..", StringComparison.Ordinal) || Directory.Exists(Path.Combine(project, file));

    static string Describe(BuildOrder order) => string.Join('\n', order.RemovedChanges.Select(change =>
    {
        var line = $"- slice {change.SliceId}";
        if (!string.IsNullOrWhiteSpace(change.PreviousName)) line += $"; former name: {change.PreviousName}";
        var declarations = change.Declarations.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
        return declarations.Count == 0 ? line : $"{line}; declarations: {string.Join(", ", declarations)}";
    }).Select(line => line.ReplaceLineEndings(" ")));
}
