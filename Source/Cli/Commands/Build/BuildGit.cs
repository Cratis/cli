// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The repository work of a build, done by the host and never by the agent: moving to the build's branch, committing the
/// agent's files, pushing, and putting away whatever a build that did not end in a commit left behind. Nothing is ever
/// discarded: leftover changes are stashed, so they can be recovered.
/// </summary>
/// <param name="project">The repository's root folder.</param>
public sealed class BuildGit(string project)
{
    /// <summary>
    /// Gets a value indicating whether the project is a Git repository.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    public async Task<bool> IsRepository(CancellationToken cancellationToken) =>
        (await Git(["rev-parse", "--is-inside-work-tree"], cancellationToken)).ExitCode == 0;

    /// <summary>
    /// Lists the paths with uncommitted changes, including untracked files.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The changed paths, relative to the repository.</returns>
    public async Task<IReadOnlyList<string>> Changes(CancellationToken cancellationToken)
    {
        var status = await Git(["status", "--porcelain=v1", "--untracked-files=all"], cancellationToken);
        return [.. status.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Length > 3)
            .Select(line => line[3..].Trim().Trim('"'))
            .Select(path => path.Contains(" -> ", StringComparison.Ordinal) ? path[(path.IndexOf(" -> ", StringComparison.Ordinal) + 4)..] : path)];
    }

    /// <summary>
    /// Gets the branch checked out, or <see langword="null"/> on a detached head.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The branch.</returns>
    public async Task<string?> CurrentBranch(CancellationToken cancellationToken)
    {
        var result = await Git(["symbolic-ref", "--quiet", "--short", "HEAD"], cancellationToken);
        return result.ExitCode == 0 ? result.Output.Trim() : null;
    }

    /// <summary>
    /// Moves to a branch: the local one if it exists, else one tracking the remote's, else a new one from where the
    /// repository is.
    /// </summary>
    /// <param name="branch">The branch.</param>
    /// <param name="cancellationToken">Cancels the move.</param>
    /// <returns>An error, or <see langword="null"/> when the branch is checked out.</returns>
    public async Task<string?> Switch(string branch, CancellationToken cancellationToken)
    {
        if (await CurrentBranch(cancellationToken) == branch) return null;
        if ((await Git(["rev-parse", "--verify", "--quiet", $"refs/heads/{branch}"], cancellationToken)).ExitCode == 0)
        {
            return Error(await Git(["switch", branch], cancellationToken));
        }

        if ((await Git(["rev-parse", "--verify", "--quiet", $"refs/remotes/origin/{branch}"], cancellationToken)).ExitCode == 0)
        {
            return Error(await Git(["switch", "--track", $"origin/{branch}"], cancellationToken));
        }

        return Error(await Git(["switch", "-c", branch], cancellationToken));
    }

    /// <summary>
    /// Commits paths with a message.
    /// </summary>
    /// <param name="paths">The paths, relative to the repository.</param>
    /// <param name="message">The commit message.</param>
    /// <param name="cancellationToken">Cancels the commit.</param>
    /// <returns>The commit, or an error.</returns>
    public async Task<(string? Commit, string? Error)> Commit(IReadOnlyList<string> paths, string message, CancellationToken cancellationToken)
    {
        if (paths.Count > 0)
        {
            var added = await Git(["add", "--all", "--", .. paths], cancellationToken);
            if (added.ExitCode != 0) return (null, Error(added));
        }

        var staged = await Git(["diff", "--cached", "--quiet"], cancellationToken);
        if (staged.ExitCode != 0)
        {
            var committed = await Git(["commit", "-m", message], cancellationToken);
            if (committed.ExitCode != 0) return (null, Error(committed));
        }

        var head = await Git(["rev-parse", "HEAD"], cancellationToken);
        return head.ExitCode == 0 ? (head.Output.Trim(), null) : (null, Error(head));
    }

    /// <summary>
    /// Pushes a branch to <c language="csharp">origin</c> with the user's own Git credentials.
    /// </summary>
    /// <param name="branch">The branch.</param>
    /// <param name="cancellationToken">Cancels the push.</param>
    /// <returns>An error, or <see langword="null"/> when it was pushed or there is no <c language="csharp">origin</c>.</returns>
    public async Task<string?> Push(string branch, CancellationToken cancellationToken)
    {
        if ((await Git(["remote", "get-url", "origin"], cancellationToken)).ExitCode != 0) return null;
        return Error(await Git(["push", "--set-upstream", "origin", branch], cancellationToken));
    }

    /// <summary>
    /// Puts away whatever uncommitted changes are left, so the next build starts clean and nothing is lost.
    /// </summary>
    /// <param name="message">The stash message.</param>
    /// <param name="keep">Paths to leave where they are, such as recorded conventions.</param>
    /// <param name="cancellationToken">Cancels the stash.</param>
    /// <returns><see langword="true"/> when something was stashed.</returns>
    public async Task<bool> PutAway(string message, IReadOnlyList<string> keep, CancellationToken cancellationToken)
    {
        if ((await Changes(cancellationToken)).All(path => keep.Contains(path, StringComparer.Ordinal))) return false;
        return (await Git(["stash", "push", "--include-untracked", "-m", message, "--", ".", .. keep.Select(path => $":(exclude){path}")], cancellationToken)).ExitCode == 0;
    }

    static string? Error(BuildProcessResult result) =>
        result.ExitCode == 0 ? null : $"git exited with {result.ExitCode}: {(result.Error.Length > 0 ? result.Error : result.Output).Trim()}";

    Task<BuildProcessResult> Git(IEnumerable<string> arguments, CancellationToken cancellationToken) =>
        BuildProcess.Run("git", arguments, project, null, null, null, cancellationToken);
}
