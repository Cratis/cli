// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The packaged Cratis builder: writes the order's Screenplay model into the project, renders it with the CLI's own renderer
/// (the Cratis or the Axon target), and, at the code-with-AI level, hands an agent what the renderer leaves for judgment. The
/// host around it, <see cref="BuildHost"/>, still verifies, commits and reports, so it is the same builder the cloud runs.
/// </summary>
/// <param name="session">The session.</param>
/// <param name="level">How much it produces.</param>
/// <param name="target">The render target.</param>
/// <param name="gapFiller">The agent that completes the render, for the code-with-AI level.</param>
public sealed class CratisLevelBuilder(BuildAgentSession session, BuildLevel level, string target, IBuildAgent? gapFiller) : IBuildAgent
{
    /// <summary>The folder in the project that holds the Screenplay model.</summary>
    public const string ModelFolder = "screenplay";

    bool _rendered;

    /// <inheritdoc/>
    public string Name => gapFiller is null ? "the Cratis builder" : $"the Cratis builder with {gapFiller.Name}";

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
    /// Gets the prompt that hands an agent the part of the build a renderer cannot do.
    /// </summary>
    /// <param name="prompt">The prompt the host filled from the order.</param>
    /// <param name="target">The render target.</param>
    /// <returns>The prompt.</returns>
    public static string GapFillPrompt(string prompt, string target) =>
        $"The Cratis {target} renderer has already generated the application's structure from the Screenplay model in the screenplay folder; " +
        "do not regenerate it and do not edit the generated files that carry a managed header except where a body is left for you. " +
        "Find the bodies the renderer left unimplemented for the slices below and complete them as the specifications in the order require, " +
        $"then run the verification commands.\n\n{prompt}";

    /// <inheritdoc/>
    public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
    {
        if (_rendered && gapFiller is not null) return await gapFiller.Prompt(prompt, environment, cancellationToken);

        var order = BuildOrder.Parse(await File.ReadAllTextAsync(environment[ExecGenerator.OrderVariable], cancellationToken));
        var model = Path.Combine(session.Project, ModelFolder);
        foreach (var document in order.Documents.Where(document => document.Path.Length > 0)) BuildFiles.WriteDocument(model, document);
        session.Log($"Wrote the Screenplay model of {order.Application} to {ModelFolder}/.");

        if (level != BuildLevel.Screenplay && await Render(order, model, cancellationToken) is { } refusal)
        {
            await File.WriteAllTextAsync(environment[ExecGenerator.OutcomeVariable], BuildOutcomes.Failure(order, refusal, false).ToJsonString(), cancellationToken);
            return new(refusal, true, null);
        }

        _rendered = true;
        if (level == BuildLevel.CodeWithAI && gapFiller is not null) return await gapFiller.Prompt(GapFillPrompt(prompt, target), environment, cancellationToken);

        var outcome = new JsonObject { ["kind"] = BuildOutcomes.Built, ["files"] = new JsonArray(), ["specifications"] = new JsonArray(), ["assumptions"] = new JsonArray() };
        await File.WriteAllTextAsync(environment[ExecGenerator.OutcomeVariable], outcome.ToJsonString(), cancellationToken);
        return new(string.Empty, true, null);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (gapFiller is not null) await gapFiller.DisposeAsync();
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
