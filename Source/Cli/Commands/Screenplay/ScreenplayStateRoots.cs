// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Finds the folder whose Screenplay workspace state must keep being served for a discovered model folder.
/// </summary>
/// <remarks>
/// The Screenplay MCP server keeps applied identities (<c language="shell">.screenplay/identities.json</c>) and interrupted-write
/// journals (<c language="shell">.screenplay/pending.json</c>) in the folder it serves. Serving a model folder nested below a
/// folder that already holds that state would silently start a new workspace, so the folder nearest the
/// project that holds state wins, the same rule the server applies to a root a client offers.
/// </remarks>
internal static class ScreenplayStateRoots
{
    const string StateFolder = ".screenplay";
    const string Identities = "identities.json";
    const string Journal = "pending.json";

    /// <summary>
    /// Chooses the folder to serve for a model discovered inside a project.
    /// </summary>
    /// <param name="project">The project directory.</param>
    /// <param name="discovered">The model folder found inside the project; it need not exist yet.</param>
    /// <param name="report">Receives a notice when more than one folder holds workspace state.</param>
    /// <returns>The nearest folder to the project holding workspace state, or <see langword="null"/> when none does.</returns>
    /// <exception cref="ScreenplayWorkspaceStateConflict">Another folder than the chosen one holds an interrupted write.</exception>
    /// <exception cref="ScreenplayWorkspaceMetadataConflict">Workspace metadata is a link or has the wrong file type.</exception>
    internal static string? Select(string project, string discovered, Action<string>? report)
    {
        var states = Between(project, discovered).Select(path => (Path: path, State: StateAt(path))).ToArray();
        var stateRoots = states.Where(entry => entry.State.HasState).Select(entry => entry.Path).ToArray();
        if (stateRoots.Length == 0) return null;

        var selected = stateRoots[0];
        var pending = states.Where(entry => entry.State.HasJournal && entry.Path != selected).Select(entry => entry.Path).ToArray();
        if (pending.Length > 0) throw new ScreenplayWorkspaceStateConflict(selected, pending);
        if (stateRoots.Length > 1)
        {
            report?.Invoke($"Serving the Screenplay workspace state in '{selected}'; {string.Join(", ", stateRoots.Skip(1).Select(path => $"'{path}'"))} also hold{(stateRoots.Length == 2 ? "s" : string.Empty)} state, which this server does not use. Pass that folder as the path to work with it.");
        }

        return selected;
    }

    /// <summary>
    /// Lists the folders from the project down to the discovered folder, so the folder nearest the project comes first.
    /// </summary>
    /// <param name="project">The project directory.</param>
    /// <param name="discovered">The discovered model folder.</param>
    /// <returns>The folders in order.</returns>
    static List<string> Between(string project, string discovered)
    {
        var paths = new List<string>();
        var root = Path.TrimEndingDirectorySeparator(project);
        for (var directory = Path.TrimEndingDirectorySeparator(discovered); directory is not null; directory = Path.GetDirectoryName(directory))
        {
            paths.Add(directory);
            if (string.Equals(directory, root, StringComparison.Ordinal)) break;
        }

        paths.Reverse();
        return paths;
    }

    static (bool HasState, bool HasJournal) StateAt(string directory)
    {
        var metadata = Path.Combine(directory, StateFolder);
        var identities = Path.Combine(metadata, Identities);
        var journal = Path.Combine(metadata, Journal);
        foreach (var path in new[] { directory, metadata, identities, journal })
        {
            if (IsLink(path)) throw new ScreenplayWorkspaceMetadataConflict($"symbolic links and reparse points are not admitted: '{path}'");
        }

        if (File.Exists(metadata) || Directory.Exists(identities) || Directory.Exists(journal))
        {
            throw new ScreenplayWorkspaceMetadataConflict($"'{metadata}' must be a directory and its {Identities} and {Journal} entries must be files");
        }

        var hasJournal = File.Exists(journal);
        return (hasJournal || File.Exists(identities), hasJournal);
    }

    /// <summary>
    /// Checks whether a path is a link; a missing path is a valid answer and never authorizes following one.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><see langword="true"/> when the path is a symbolic link or reparse point.</returns>
    static bool IsLink(string path) =>
        (File.Exists(path) || Directory.Exists(path) || new FileInfo(path).LinkTarget is not null) &&
        File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
