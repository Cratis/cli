// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Builds one order: writes it to a workspace outside the project, hands the agent the filled prompt with the kit's
/// instructions, the guidance and the tools, reads the outcome the agent wrote, runs every verification command, and does
/// the repository work. A failing verification command makes the outcome failed, whatever the agent wrote.
/// </summary>
/// <param name="options">How the host builds.</param>
public sealed class BuildHost(BuildHostOptions options)
{
    /// <summary>How many times an agent that stopped without an outcome is asked to go on before the build fails.</summary>
    const int MaximumContinuations = 2;

    readonly BuildGit _git = new(options.Project);

    /// <summary>
    /// Builds an order.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="git">Who does the repository work, and how.</param>
    /// <param name="cancellationToken">Stops the build, for example when the lease is lost.</param>
    /// <returns>The outcome to report, free of credentials.</returns>
    public async Task<JsonObject> Build(BuildOrder order, BuildGitInstructions git, CancellationToken cancellationToken)
    {
        if (Refusal(order) is { } refusal) return BuildOutcomes.Failure(order, refusal, false);

        var workspace = BuildWorkspace.Create(options.WorkspaceRoot, order);
        var repository = await _git.IsRepository(cancellationToken);
        var original = repository ? await _git.CurrentBranch(cancellationToken) : null;
        try
        {
            if (repository && await PrepareBranch(order, git, cancellationToken) is { } error) return BuildOutcomes.Failure(order, error, true);

            var conventions = await new BuildConventions(options).Ensure(order, workspace, cancellationToken);
            var values = BuildKitPlaceholders.For(order, workspace.Order, workspace.Outcome, conventions ?? string.Empty);
            var outcome = await RunAgent(order, workspace, values, cancellationToken);

            var kind = outcome["kind"]?.GetValue<string>();
            if ((string.Equals(kind, BuildOutcomes.Built, StringComparison.Ordinal) || string.Equals(kind, BuildOutcomes.NothingToDo, StringComparison.Ordinal)) &&
                await new BuildVerification(options).Run(order, values, cancellationToken) is { } failure)
            {
                outcome = BuildOutcomes.Failure(order, failure, true);
            }

            if (repository && outcome["kind"]?.GetValue<string>() == BuildOutcomes.Built)
            {
                outcome = await Commit(order, git, outcome, values, cancellationToken);
            }

            return options.Redaction.Redact(outcome);
        }
        finally
        {
            if (repository)
            {
                var keep = options.Kit?.Conventions is { } recorded ? new[] { recorded } : [];
                if (await _git.PutAway($"cratis build: left over from order {order.OrderId}", keep, CancellationToken.None)) options.Log("Put the build's uncommitted changes away in a Git stash.");
                if (original is not null) await _git.Switch(original, CancellationToken.None);
            }
        }
    }

    string? Refusal(BuildOrder order)
    {
        if (options.Kit is not { } kit) return null;
        if (order.Slices.FirstOrDefault(slice => !kit.SliceTypes.Contains(slice.Type)) is { } unsupported)
        {
            return $"The kit {kit.Name} does not build {unsupported.Type} slices such as {unsupported.Name}.";
        }

        return order.Part == "ui" && !kit.BuildsUserInterfaces ? $"The kit {kit.Name} does not build user interfaces." : null;
    }

    async Task<string?> PrepareBranch(BuildOrder order, BuildGitInstructions git, CancellationToken cancellationToken)
    {
        var branch = BranchFor(order, git);
        return branch is null ? null : await _git.Switch(branch, cancellationToken);
    }

    string? BranchFor(BuildOrder order, BuildGitInstructions git)
    {
        if (git.IsStudioManaged) return git.Branch;
        return options.Kit?.GitBranch is { } template
            ? BuildBranchNames.From(template, BuildKitPlaceholders.For(order, string.Empty, string.Empty, string.Empty))
            : null;
    }

    async Task<JsonObject> RunAgent(BuildOrder order, BuildWorkspace workspace, IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken)
    {
        var kit = options.Kit;
        var session = new BuildAgentSession(
            options.Project,
            workspace.Root,
            kit is null ? string.Empty : kit.ReadText(kit.Instructions),
            options.Guidance,
            kit?.Skills is { } skills ? kit.PathOf(skills) : null,
            options.Tools(workspace.Model),
            Permissions(workspace, values),
            line => options.Log(options.Redaction.Redact(line)));

        var prompt = BuildPrompts.ForOrder(kit?.ReadText(kit.Prompt), values);
        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ExecGenerator.OrderVariable] = workspace.Order,
            [ExecGenerator.OutcomeVariable] = workspace.Outcome
        };

        await using var agent = options.Agent(session);
        var reply = await agent.Prompt(prompt, environment, cancellationToken);
        for (var nudge = 0; reply.Succeeded && !File.Exists(workspace.Outcome) && nudge < MaximumContinuations; nudge++)
        {
            options.Log($"{agent.Name} stopped without an outcome; asking it to go on.");
            reply = await agent.Prompt(BuildPrompts.ToContinue(workspace.Outcome), environment, cancellationToken);
        }

        if (!reply.Succeeded) return BuildOutcomes.Failure(order, reply.Error ?? $"{agent.Name} did not finish.", true);
        return BuildOutcomes.FromAgent(order, File.Exists(workspace.Outcome) ? await File.ReadAllTextAsync(workspace.Outcome, cancellationToken) : null);
    }

    BuildPermissions Permissions(BuildWorkspace workspace, IReadOnlyDictionary<string, string> values)
    {
        var kit = options.Kit;
        var verify = (kit?.Verify ?? []).Concat(options.Verify).Select(command => BuildKitPlaceholders.FillCommand(command, values));
        return new(
            options.Project,
            kit?.PermittedPaths ?? ["**"],
            [.. (kit?.PermittedCommands ?? []).Concat(verify)],
            [workspace.Root, .. options.Guidance is { } guidance ? [guidance.Root] : Array.Empty<string>()],
            [workspace.Outcome]);
    }

    async Task<JsonObject> Commit(BuildOrder order, BuildGitInstructions git, JsonObject outcome, IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken)
    {
        var permissions = Permissions(BuildWorkspace.At(options.WorkspaceRoot, order), values);
        var changed = await _git.Changes(cancellationToken);
        var files = (outcome["files"] as JsonArray ?? []).Select(file => file?.ToString()).OfType<string>().ToList();
        var paths = changed.Where(path =>
            path == options.Kit?.Conventions ||
            (permissions.AllowsWrite(path) && (files.Count == 0 || files.Contains(path, StringComparer.Ordinal) || files.Exists(file => path.StartsWith(file.TrimEnd('/') + "/", StringComparison.Ordinal))))).ToList();
        var message = git.IsStudioManaged && git.CommitMessage is { } studioMessage
            ? studioMessage
            : BuildKitPlaceholders.Fill(options.Kit?.GitCommit ?? "Build {slices} ({part})", values);

        var (commit, error) = await _git.Commit(paths, message, cancellationToken);
        if (commit is null) return BuildOutcomes.Failure(order, $"The work could not be committed: {error}", true);

        var branch = await _git.CurrentBranch(cancellationToken);
        if (options.Push && branch is not null && await _git.Push(branch, cancellationToken) is { } pushError)
        {
            return BuildOutcomes.Failure(order, $"The commit could not be pushed: {pushError}", true);
        }

        outcome["commit"] = commit;
        if (branch is not null) outcome["branch"] = branch;
        options.Log($"Committed {commit[..Math.Min(7, commit.Length)]}{(branch is null ? string.Empty : $" on {branch}")}.");
        return outcome;
    }
}
