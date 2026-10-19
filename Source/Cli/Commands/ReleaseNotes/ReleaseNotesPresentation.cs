// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Defines how <see cref="ReleaseNotesCommand"/> presents the releases it selected.
/// </summary>
public enum ReleaseNotesPresentation
{
    /// <summary>
    /// Show nothing: there are no releases to show.
    /// </summary>
    Nothing,

    /// <summary>
    /// Write the releases as JSON.
    /// </summary>
    Json,

    /// <summary>
    /// Write the version of each release, one per line.
    /// </summary>
    Versions,

    /// <summary>
    /// Write the markdown of each release as is.
    /// </summary>
    Markdown,

    /// <summary>
    /// Write each release, formatted for the terminal.
    /// </summary>
    Formatted,

    /// <summary>
    /// Page through the releases one at a time.
    /// </summary>
    Pager,

    /// <summary>
    /// Ask whether to show the releases, then page through them.
    /// </summary>
    AskThenPager
}
