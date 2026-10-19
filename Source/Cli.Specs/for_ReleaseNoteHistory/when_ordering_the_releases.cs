// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReleaseNoteHistory;

public class when_ordering_the_releases : given.a_release_note_history
{
    [Fact] void should_put_the_newest_first_comparing_versions_numerically() => _history.Notes.Select(_ => _.Version).ToArray().ShouldEqual(["3.86.0", "3.85.0", "3.84.0", "3.9.0"]);
    [Fact] void should_find_a_release_by_its_tag() => _history.Find("v3.85.0")!.Notes.ShouldEqual("Eighty-five");
}
