// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// An <see cref="IGitRepository"/> that runs the git executable.
/// </summary>
/// <param name="root">The working tree root.</param>
public class GitRepository(string root) : IGitRepository
{
    /// <inheritdoc/>
    public string Root { get; } = root;

    /// <summary>
    /// Finds the repository that contains a directory.
    /// </summary>
    /// <param name="directory">Any directory inside the working tree.</param>
    /// <returns>The repository.</returns>
    /// <exception cref="AffectedException">The directory is not inside a git repository.</exception>
    public static GitRepository Open(string directory)
    {
        var result = ProcessRunner.Run("git", ["rev-parse", "--show-toplevel"], directory);
        if (result.ExitCode != 0)
        {
            throw new AffectedException($"'{directory}' is not inside a git repository: {result.Error.Trim()}");
        }

        return new GitRepository(result.Output.Trim());
    }

    /// <inheritdoc/>
    public string ResolveCommit(string revision) => Require("rev-parse", "--verify", $"{revision}^{{commit}}").Trim();

    /// <inheritdoc/>
    public bool IsAncestor(string ancestor, string descendant) =>
        ProcessRunner.Run("git", ["merge-base", "--is-ancestor", ancestor, descendant], Root).ExitCode == 0;

    /// <inheritdoc/>
    public string MergeBase(string first, string second) => Require("merge-base", first, second).Trim();

    /// <inheritdoc/>
    public IReadOnlyList<FileChange> Diff(string from, string to)
    {
        var fields = Require("diff", "--name-status", "-z", "--find-renames=50%", "--no-ext-diff", from, to)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var changes = new List<FileChange>();
        for (var index = 0; index < fields.Length;)
        {
            var status = fields[index++];
            switch (status[0])
            {
                case 'R':
                    changes.Add(new FileChange(ChangeKind.Renamed, fields[index + 1], fields[index]));
                    index += 2;
                    break;
                case 'C':
                    changes.Add(new FileChange(ChangeKind.Added, fields[index + 1]));
                    index += 2;
                    break;
                case 'A':
                    changes.Add(new FileChange(ChangeKind.Added, fields[index++]));
                    break;
                case 'D':
                    changes.Add(new FileChange(ChangeKind.Deleted, fields[index++]));
                    break;
                default:
                    changes.Add(new FileChange(ChangeKind.Modified, fields[index++]));
                    break;
            }
        }

        return changes;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ListFiles(string revision) =>
        Require("ls-tree", "-r", "--name-only", "-z", revision).Split('\0', StringSplitOptions.RemoveEmptyEntries);

    /// <inheritdoc/>
    public string? ReadFile(string revision, string path)
    {
        var result = ProcessRunner.Run("git", ["show", $"{revision}:{path}"], Root);
        return result.ExitCode == 0 ? result.Output : null;
    }

    string Require(params string[] arguments)
    {
        var result = ProcessRunner.Run("git", arguments, Root);
        if (result.ExitCode != 0)
        {
            throw new AffectedException($"git {string.Join(' ', arguments)} failed: {result.Error.Trim()}");
        }

        return result.Output;
    }
}
