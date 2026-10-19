// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNoteHistory.given;

public class a_release_note_history : Specification
{
    protected ReleaseNoteHistory _history;

    void Establish() => _history = new(
    [
        new("3.84.0", "Eighty-four"),
        new("v3.86.0", "Eighty-six"),
        new("3.85.0", "Eighty-five"),
        new("3.9.0", "Nine")
    ]);
}
