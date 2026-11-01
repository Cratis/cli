// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNoteHistory.when_selecting_between_versions;

public class and_several_versions_separate_them : given.a_release_note_history
{
    IReadOnlyList<ReleaseNote> _result;

    void Because() => _result = _history.Between("3.9.0", "3.86.0+abc");

    [Fact] void should_select_every_release_after_the_previous_up_to_the_new_one_newest_first() => _result.Select(_ => _.Version).ToArray().ShouldEqual(["3.86.0", "3.85.0", "3.84.0"]);
}
