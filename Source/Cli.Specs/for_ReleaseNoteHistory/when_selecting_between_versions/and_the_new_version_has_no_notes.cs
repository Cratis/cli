// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNoteHistory.when_selecting_between_versions;

public class and_the_new_version_has_no_notes : given.a_release_note_history
{
    IReadOnlyList<ReleaseNote> _result;

    void Because() => _result = _history.Between("3.85.0", "3.87.0");

    [Fact] void should_list_the_new_version_first() => _result[0].Version.ShouldEqual("3.87.0");
    [Fact] void should_have_no_notes_for_it() => _result[0].Notes.ShouldBeNull();
    [Fact] void should_point_at_its_release_page() => _result[0].Url.ShouldEqual("https://github.com/Cratis/cli/releases/tag/v3.87.0");
    [Fact] void should_still_list_the_releases_it_has_notes_for() => _result.Skip(1).Select(_ => _.Version).ToArray().ShouldEqual(["3.86.0"]);
}
