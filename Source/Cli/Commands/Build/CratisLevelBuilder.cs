// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The packaged Cratis builder: writes the order's Screenplay model into the project and, by level, renders it with the CLI's
/// own renderer (the Cratis or the Axon target), hands an agent the behavior the model describes to implement on top of the
/// render, or hands an agent the whole implementation. The host around it, <see cref="BuildHost"/>, still verifies, commits and
/// reports, so it is the same builder the cloud runs.
/// </summary>
/// <param name="session">The session.</param>
/// <param name="level">How much it produces.</param>
/// <param name="target">The render target.</param>
/// <param name="implementer">The agent that writes code, for the code-with-AI and AI levels.</param>
public sealed class CratisLevelBuilder(BuildAgentSession session, BuildLevel level, string target, IBuildAgent? implementer) : IBuildAgent
{
    /// <summary>The folder in the project that holds the Screenplay model.</summary>
    public const string ModelFolder = "screenplay";

    /// <summary>
    /// What an agent is told to implement, at the levels where one writes code: everything the model describes beside its structure.
    /// </summary>
    public const string ModelBehavior =
        "Implement the behavior the model describes: what each slice is supposed to do, from the slice descriptions in the order's context.hints; " +
        "the command handlers and query descriptions written in the Screenplay documents; the order's specifications, each an acceptance criterion; " +
        "and the feedback people left on the slices in context.feedback. They are listed under 'Instructions from the model' below when the order carries them. " +
        "Do not invent behavior the model does not describe: where it leaves a decision open, make the smallest one and record it as an assumption in the outcome.";

    readonly SortedSet<string> _preparedFiles = [with(StringComparer.Ordinal)];
    bool _prepared;

    /// <inheritdoc/>
    public string Name => implementer is null ? "the Cratis builder" : $"the Cratis builder with {implementer.Name}";

    /// <summary>
    /// Gets the arguments that render a model with this CLI.
    /// </summary>
    /// <param name="model">The folder holding the model.</param>
    /// <param name="application">The application's name.</param>
    /// <param name="destination">The folder to render into.</param>
    /// <param name="target">The render target.</param>
    /// <returns>The arguments after <c language="csharp">cratis</c>.</returns>
    public static IReadOnlyList<string> RenderArguments(string model, string application, string destination, string target) =>
        ["render", model, "--name", application, "--destination", destination, "--target", target.ToLowerInvariant()];

    /// <summary>
    /// Gets the prompt that hands an agent the behavior to implement on top of a deterministic render, at the code-with-AI level.
    /// </summary>
    /// <param name="prompt">The prompt the host filled from the order, with its instructions from the model.</param>
    /// <param name="target">The render target.</param>
    /// <returns>The prompt.</returns>
    public static string RefinePrompt(string prompt, string target) =>
        $"The Cratis {target} renderer has already generated the application's structure from the Screenplay model in the {ModelFolder} folder. " +
        "Do not regenerate it, and do not edit the files that carry a managed header except the places the renderer left for you. " +
        $"{ModelBehavior} " +
        $"Build it on top of the generated structure: complete the bodies the renderer left unimplemented{LeftBodies(target)}, " +
        "and add code in new files, or in files without a managed header, where the behavior needs it. " +
        $"Then run the verification commands.\n\n{prompt}";

    /// <summary>
    /// Gets the prompt that hands an agent the whole implementation, at the AI level: no renderer runs.
    /// </summary>
    /// <param name="prompt">The prompt the host filled from the order, with its instructions from the model.</param>
    /// <param name="target">The target, which says what the application is written in.</param>
    /// <returns>The prompt.</returns>
    public static string AiOnlyPrompt(string prompt, string target) =>
        $"The Screenplay model of the application is in the {ModelFolder} folder and no renderer has run: implement the slices below from it yourself, " +
        $"as {Stack(target)}, following the build kit's instructions and conventions and the patterns already in the project. " +
        $"Do not change the {ModelFolder} folder; it is the source of truth. " +
        $"{ModelBehavior} " +
        $"Then run the verification commands.\n\n{prompt}";

    /// <summary>
    /// Adds the files this builder wrote before the agent's turn, the model and the render, to an outcome that lists the agent's
    /// files: the host commits only the files an outcome lists, and the agent reports only its own.
    /// </summary>
    /// <param name="outcome">The outcome document.</param>
    /// <param name="preparedFiles">The files the builder wrote, relative to the project.</param>
    /// <returns>The outcome, with the prepared files added when it lists files.</returns>
    public static JsonObject WithPreparedFiles(JsonObject outcome, IEnumerable<string> preparedFiles)
    {
        if (outcome["files"] is not JsonArray files || files.Count == 0) return outcome;
        var listed = files.Select(file => file?.ToString()).OfType<string>().ToHashSet(StringComparer.Ordinal);
        foreach (var file in preparedFiles.Where(listed.Add)) files.Add(file);
        return outcome;
    }

    /// <inheritdoc/>
    public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
    {
        // A prompt without an order learns the project's conventions: it is the agent's, as is every prompt once the model is in place.
        if (!environment.ContainsKey(ExecGenerator.OrderVariable))
        {
            return implementer is null ? new(string.Empty, true, null) : await implementer.Prompt(prompt, environment, cancellationToken);
        }

        if (_prepared) return await Implement(prompt, environment, cancellationToken);

        var order = BuildOrder.Parse(await File.ReadAllTextAsync(environment[ExecGenerator.OrderVariable], cancellationToken));
        var model = Path.Combine(session.Project, ModelFolder);
        var before = ProjectFiles.Snapshot(session.Project);
        foreach (var document in order.Documents.Where(document => document.Path.Length > 0)) BuildFiles.WriteDocument(model, document);
        session.Log($"Building at the {BuildLevels.Names[(int)level]} level ({BuildLevels.StudioMode(level)}); wrote the Screenplay model of {order.Application} to {ModelFolder}/.");

        if (BuildLevels.Renders(level) && await Render(order, model, cancellationToken) is { } refusal)
        {
            await File.WriteAllTextAsync(environment[ExecGenerator.OutcomeVariable], BuildOutcomes.Failure(order, refusal, false).ToJsonString(), cancellationToken);
            return new(refusal, true, null);
        }

        _prepared = true;
        _preparedFiles.UnionWith(ProjectFiles.Changed(before, ProjectFiles.Snapshot(session.Project)));
        if (implementer is not null && level == BuildLevel.CodeWithAI) return await Implement(RefinePrompt(prompt, target), environment, cancellationToken);
        if (implementer is not null && level == BuildLevel.AI) return await Implement(AiOnlyPrompt(prompt, target), environment, cancellationToken);

        var outcome = new JsonObject
        {
            ["kind"] = BuildOutcomes.Built,
            ["files"] = new JsonArray([.. _preparedFiles.Select(file => (JsonNode)file)]),
            ["specifications"] = new JsonArray(),
            ["assumptions"] = new JsonArray()
        };
        await File.WriteAllTextAsync(environment[ExecGenerator.OutcomeVariable], outcome.ToJsonString(), cancellationToken);
        return new(string.Empty, true, null);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (implementer is not null) await implementer.DisposeAsync();
    }

    static JsonObject? TryParse(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    static string LeftBodies(string target) => string.Equals(target, BuildLevels.AxonTarget, StringComparison.OrdinalIgnoreCase)
        ? " (the Axon renderer lists them in UNSUPPORTED.md and runtime/UnsupportedImplementations.java at the project root)"
        : string.Empty;

    static string Stack(string target) => string.Equals(target, BuildLevels.AxonTarget, StringComparison.OrdinalIgnoreCase)
        ? "a Java application on Axon Framework"
        : "a .NET application on Cratis Arc and Chronicle with a React frontend";

    async Task<BuildAgentReply> Implement(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
    {
        if (implementer is null) return new(string.Empty, true, null);
        var reply = await implementer.Prompt(prompt, environment, cancellationToken);
        var outcomePath = environment[ExecGenerator.OutcomeVariable];
        if (_preparedFiles.Count > 0 && File.Exists(outcomePath) && TryParse(await File.ReadAllTextAsync(outcomePath, cancellationToken)) is { } outcome)
        {
            await File.WriteAllTextAsync(outcomePath, WithPreparedFiles(outcome, _preparedFiles).ToJsonString(), cancellationToken);
        }

        return reply;
    }

    async Task<string?> Render(BuildOrder order, string model, CancellationToken cancellationToken)
    {
        var (program, leading) = BuildSessionTools.CurrentCli();
        var arguments = leading.Concat(RenderArguments(model, order.Application, session.Project, target));
        session.Log($"Rendering {order.Application} with the {target} renderer.");
        var result = await BuildProcess.Run(program, arguments, session.Project, onLine: session.Log, cancellationToken: cancellationToken);
        return result.ExitCode == 0 ? null : $"The {target} renderer refused the model (exit {result.ExitCode}): {(result.Output + result.Error).Trim()[^Math.Min(800, (result.Output + result.Error).Trim().Length)..]}";
    }
}
