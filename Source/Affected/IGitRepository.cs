// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The read-only view of a git repository that affected-test computation needs.
/// </summary>
public interface IGitRepository
{
    /// <summary>
    /// Gets the absolute path of the working tree root.
    /// </summary>
    string Root { get; }

    /// <summary>
    /// Resolves a revision to a full commit id.
    /// </summary>
    /// <param name="revision">Any revision git understands.</param>
    /// <returns>The commit id.</returns>
    string ResolveCommit(string revision);

    /// <summary>
    /// Gets whether one commit is an ancestor of, or equal to, another. Unknown commits, such as after a force push or in a shallow clone, are not ancestors.
    /// </summary>
    /// <param name="ancestor">The possible ancestor.</param>
    /// <param name="descendant">The possible descendant.</param>
    /// <returns>True when <paramref name="ancestor"/> is reachable from <paramref name="descendant"/>.</returns>
    bool IsAncestor(string ancestor, string descendant);

    /// <summary>
    /// Finds the best common ancestor of two revisions.
    /// </summary>
    /// <param name="first">The first revision.</param>
    /// <param name="second">The second revision.</param>
    /// <returns>The merge base commit id.</returns>
    string MergeBase(string first, string second);

    /// <summary>
    /// Lists the files that changed between two revisions, with rename detection.
    /// </summary>
    /// <param name="from">The earlier revision.</param>
    /// <param name="to">The later revision.</param>
    /// <returns>The changes.</returns>
    IReadOnlyList<FileChange> Diff(string from, string to);

    /// <summary>
    /// Lists every tracked file at a revision.
    /// </summary>
    /// <param name="revision">The revision.</param>
    /// <returns>Repository relative paths with forward slashes.</returns>
    IReadOnlyList<string> ListFiles(string revision);

    /// <summary>
    /// Reads a file as it was at a revision.
    /// </summary>
    /// <param name="revision">The revision.</param>
    /// <param name="path">The repository relative path.</param>
    /// <returns>The text, or null when the file did not exist.</returns>
    string? ReadFile(string revision, string path);
}
