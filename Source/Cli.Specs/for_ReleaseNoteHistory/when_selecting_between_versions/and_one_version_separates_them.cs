// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNoteHistory.when_selecting_between_versions;

public class and_one_version_separates_them : given.a_release_note_history
{
    IReadOnlyList<ReleaseNote> _result;

    void Because() => _result = _history.Between("3.85.0", "3.86.0");

    [Fact] void should_select_only_the_new_version() => _result.Select(_ => _.Version).ToArray().ShouldEqual(["3.86.0"]);
}
