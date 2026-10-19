// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Tracks which release a person is reading while paging through release notes one version at a time.
/// </summary>
/// <param name="notes">The releases to page through, newest first. Paging starts at the newest.</param>
public class ReleaseNotesNavigator(IReadOnlyList<ReleaseNote> notes)
{
    int _index;

    /// <summary>
    /// Gets the release being read.
    /// </summary>
    public ReleaseNote Current => notes[_index];

    /// <summary>
    /// Gets the one-based position of the release being read.
    /// </summary>
    public int Position => _index + 1;

    /// <summary>
    /// Gets the number of releases that can be paged through.
    /// </summary>
    public int Count => notes.Count;

    /// <summary>
    /// Gets a value indicating whether there is an older release to move to.
    /// </summary>
    public bool HasOlder => _index < notes.Count - 1;

    /// <summary>
    /// Gets a value indicating whether there is a newer release to move to.
    /// </summary>
    public bool HasNewer => _index > 0;

    /// <summary>
    /// Applies a key press: right, <c language="csharp">n</c>, space or page down moves to the older release;
    /// left, <c language="csharp">p</c> or page up to the newer one; home and end to the newest and oldest;
    /// <c language="csharp">q</c> or escape quits. Moving past either end stays where it is.
    /// </summary>
    /// <param name="key">The key pressed.</param>
    /// <returns>False when the person asked to quit, otherwise true.</returns>
    public bool Handle(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.RightArrow or ConsoleKey.N or ConsoleKey.Spacebar or ConsoleKey.PageDown:
                _index = Math.Min(_index + 1, notes.Count - 1);
                return true;
            case ConsoleKey.LeftArrow or ConsoleKey.P or ConsoleKey.PageUp:
                _index = Math.Max(_index - 1, 0);
                return true;
            case ConsoleKey.Home:
                _index = 0;
                return true;
            case ConsoleKey.End:
                _index = notes.Count - 1;
                return true;
            case ConsoleKey.Q or ConsoleKey.Escape:
                return false;
            default:
                return true;
        }
    }
}
