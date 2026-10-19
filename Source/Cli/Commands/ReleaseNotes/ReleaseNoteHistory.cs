// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Represents the release notes of every released version, newest first.
/// </summary>
public class ReleaseNoteHistory
{
    /// <summary>
    /// The prefix of the embedded resources holding one release's notes each, named <c language="csharp">ReleaseNotes/&lt;version&gt;.md</c>.
    /// </summary>
    internal const string ResourcePrefix = "ReleaseNotes/";

    readonly Dictionary<string, ReleaseNote> _byVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseNoteHistory"/> class.
    /// </summary>
    /// <param name="notes">The release notes, in any order.</param>
    public ReleaseNoteHistory(IEnumerable<ReleaseNote> notes)
    {
        Notes = [.. notes
            .Select(_ => _ with { Version = ReleaseNoteVersion.Normalize(_.Version) })
            .OrderByDescending(_ => _.Version, Comparer<string>.Create(ReleaseNoteVersion.Compare))];
        _byVersion = Notes.ToDictionary(_ => _.Version, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the release notes, newest first.
    /// </summary>
    public IReadOnlyList<ReleaseNote> Notes { get; }

    /// <summary>
    /// Loads the release notes embedded in this build of the CLI.
    /// </summary>
    /// <returns>The embedded <see cref="ReleaseNoteHistory"/>.</returns>
    public static ReleaseNoteHistory Embedded()
    {
        var assembly = typeof(ReleaseNoteHistory).Assembly;
        var notes = assembly.GetManifestResourceNames()
            .Where(_ => _.StartsWith(ResourcePrefix, StringComparison.Ordinal) && _.EndsWith(".md", StringComparison.Ordinal))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                return new ReleaseNote(name[ResourcePrefix.Length..^".md".Length], reader.ReadToEnd().Trim());
            });

        return new(notes);
    }

    /// <summary>
    /// Finds the notes of one version.
    /// </summary>
    /// <param name="version">The version, with or without a leading <c language="csharp">v</c>.</param>
    /// <returns>The <see cref="ReleaseNote"/>, or null when this build has none for the version.</returns>
    public ReleaseNote? Find(string version) => _byVersion.GetValueOrDefault(ReleaseNoteVersion.Normalize(version));

    /// <summary>
    /// Selects what changed when going from one version to another: every release after
    /// <paramref name="from"/> up to and including <paramref name="to"/>, newest first.
    /// </summary>
    /// <param name="from">The version updated from.</param>
    /// <param name="to">The version updated to.</param>
    /// <returns>
    /// The releases in between, newest first; nothing for a downgrade or the same version. When this build has no notes
    /// for <paramref name="to"/> itself, it is listed first without notes, so its release page can be offered instead.
    /// </returns>
    public IReadOnlyList<ReleaseNote> Between(string from, string to)
    {
        if (ReleaseNoteVersion.Compare(to, from) <= 0)
        {
            return [];
        }

        var between = Notes
            .Where(_ => ReleaseNoteVersion.Compare(_.Version, from) > 0 && ReleaseNoteVersion.Compare(_.Version, to) <= 0)
            .ToList();

        if (Find(to) is null)
        {
            between.Insert(0, new ReleaseNote(ReleaseNoteVersion.Normalize(to), null));
        }

        return between;
    }
}
